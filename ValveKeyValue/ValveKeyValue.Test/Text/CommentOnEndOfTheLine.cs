using System.Linq;
using System.Text;

namespace ValveKeyValue.Test
{
    class CommentOnEndOfTheLine
    {
        [Test]
        public void CanHandleCommentOnEndOfTheLine()
        {
            var text = new StringBuilder();
            text.Append(@"""test_kv""" + "\n");
            text.Append("{" + "\n");
            text.Append("//" + "\n");
            text.Append(@"""test""	""hello""" + "\n");
            text.Append("}" + "\n");

            var data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(text.ToString());

            Assert.Multiple(() =>
            {
                Assert.That(data.Children.Count(), Is.EqualTo(1));
                Assert.That((string)data["test"], Is.EqualTo("hello"));
            });
        }
    }
}
