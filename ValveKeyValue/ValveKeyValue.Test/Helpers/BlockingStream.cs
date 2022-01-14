using System;
using System.IO;
using System.Text;

namespace ValveKeyValue.Test.Helpers;

/// <summary>
///     Example implementation of <see cref="Stream" /> that reads at most <see cref="MaxReadAtOnce" /> characters during a
///     single <see cref="Read" /> operation, blocking in the process.
/// </summary>
internal sealed class BlockingStream : Stream
{
    private static readonly byte[] TestData = Encoding.UTF8.GetBytes(
        @"""test_kv""
		{
			""test""	""1337""
		}"
    );

    private long _backingPosition;

    internal BlockingStream(int maxReadAtOnce = 1)
    {
        if (maxReadAtOnce <= 0) throw new ArgumentOutOfRangeException(nameof(maxReadAtOnce));

        MaxReadAtOnce = maxReadAtOnce;
    }

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
        if (count > TestData.Length - _backingPosition) count = TestData.Length - (int) _backingPosition;

        // Never read more than user-configured limit
        if (count > MaxReadAtOnce) count = MaxReadAtOnce;

        for (var i = 0; i < count; i++) buffer[offset++] = TestData[_backingPosition++];

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