using System.Globalization;
using System.Text;

namespace ValveKeyValue.Test;

internal class StreamsTestCase
{
    class BlockingStream : Stream
    {
        private int _backingPosition;

        internal BlockingStream(byte[] testData, int maxReadAtOnce = 1)
        {
            ArgumentNullException.ThrowIfNull(testData);

            if (maxReadAtOnce <= 0) throw new ArgumentOutOfRangeException(nameof(maxReadAtOnce));

            TestData = testData;
            MaxReadAtOnce = maxReadAtOnce;
        }

        public byte[] TestData { get; }
        public int MaxReadAtOnce { get; }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => TestData.Length;

        public override long Position
        {
            get => _backingPosition;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));

            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));

            if (_backingPosition >= TestData.Length) return 0;

            // Never read more than we have
            if (count > TestData.Length - _backingPosition) count = TestData.Length - _backingPosition;

            // Never read more than user-configured limit
            if (count > MaxReadAtOnce) count = MaxReadAtOnce;

            Buffer.BlockCopy(TestData, _backingPosition, buffer, offset, count);

            _backingPosition += count;

            return count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }

    [TestCase(1)]
    [TestCase(10)]
    public void CanHandleBlockingStreams(int maxReadAtOnce)
    {
        var testData = Encoding.UTF8.GetBytes(
            @"""test_kv""
		{
			""test""	""1337""
		}"
        );

        using var blockingStream = new BlockingStream(testData, maxReadAtOnce);

        var data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(blockingStream);

        Assert.That(data["test"].ToInt32(CultureInfo.InvariantCulture), Is.EqualTo(1337));
    }

    [Test]
    public void LeavesStreamOpenAfterDeserialize()
    {
        using var stream = TestDataHelper.OpenResource("Text.object_person.vdf");

        KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream);

        Assert.That(stream.CanRead, Is.True);
    }

    [Test]
    public void LeavesStreamOpenAfterDeserializeBinary()
    {
        var data = new byte[]
        {
            0x00, // object: TestObject
                0x54, 0x00,
                0x08, // end object
            0x08, // end document
        };

        using var stream = new MemoryStream(data);

        KVSerializer.Create(KVSerializationFormat.KeyValues1Binary).Deserialize(stream);

        Assert.That(stream.CanRead, Is.True);
    }
}
