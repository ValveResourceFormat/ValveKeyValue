using System.Globalization;
using NUnit.Framework;
using ValveKeyValue.Test.Helpers;

namespace ValveKeyValue.Test;

internal class StreamsTestCase
{
    [TestCase(1)]
    [TestCase(4096)]
    public void CanHandleBlockingStreams(int maxReadAtOnce)
    {
        using var blockingStream = new BlockingStream(maxReadAtOnce);

        var data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(blockingStream);

        Assert.That(data["test"].ToInt32(CultureInfo.InvariantCulture), Is.EqualTo(1337));
    }
}