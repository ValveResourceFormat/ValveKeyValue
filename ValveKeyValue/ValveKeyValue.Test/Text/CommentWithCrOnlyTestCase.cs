using System.Linq;
using System.Text;

namespace ValveKeyValue.Test
{
    class CommentWithCrOnlyTestCase
    {
        [Test]
        public void CommentWithCarriageReturn()
        {
            var text = new StringBuilder();
            text.AppendLine(@"""test_kv""");
            text.AppendLine("{");
            text.AppendLine("// this is a comment that contains a carriage return: \r [$INVALID] which should continue parsing");
            text.AppendLine(@"""test""	""hello""");
            text.AppendLine("}");

            var data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(text.ToString());

            Assert.Multiple(() =>
            {
                Assert.That(data.Children.Count(), Is.EqualTo(1));
                Assert.That((string)data["test"], Is.EqualTo("hello"));
            });
        }
    }
}
