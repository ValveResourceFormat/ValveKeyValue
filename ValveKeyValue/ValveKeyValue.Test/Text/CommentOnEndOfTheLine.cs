using System.Linq;
using System.Text;
using NUnit.Framework;

namespace ValveKeyValue.Test
{
    class CommentOnEndOfTheLine
    {
        [Test]
        public void CanHandleCommentOnEndOfTheLine()
        {
            var text = new StringBuilder();
            text.AppendLine(@"""test_kv""");
            text.AppendLine("{");
            text.AppendLine("//");
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
