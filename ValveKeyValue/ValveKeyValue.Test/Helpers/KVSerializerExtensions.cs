using System.Text;
using ValveKeyValue.Metadata;

namespace ValveKeyValue.Test
{
    // Calls through these helpers have an open generic type argument, so they are not intercepted and use reflection
    // unless type information is passed.
    static class KVSerializerExtensions
    {
        public static KVDocument Deserialize(this KVSerializer serializer, byte[] data, KVSerializerOptions? options = null)
        {
            using var ms = new MemoryStream(data);
            return serializer.Deserialize(ms, options);
        }

        public static TObject Deserialize<TObject>(this KVSerializer serializer, byte[] data, KVSerializerOptions? options = null)
        {
            using var ms = new MemoryStream(data);
            return serializer.Deserialize<TObject>(ms, options);
        }

        public static KVDocument Deserialize(this KVSerializer serializer, string text, KVSerializerOptions? options = null)
        {
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(text));
            return serializer.Deserialize(ms, options);
        }

        public static TObject Deserialize<TObject>(this KVSerializer serializer, string text, KVSerializerOptions? options = null)
        {
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(text));
            return serializer.Deserialize<TObject>(ms, options);
        }

        public static TObject Deserialize<TObject>(this KVSerializer serializer, string text, KVTypeInfo<TObject> typeInfo)
        {
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(text));
            return serializer.Deserialize(ms, typeInfo);
        }

        // Returns the serialized text, which is written as UTF-8.
        public static string Serialize<T>(this KVSerializer serializer, T value, string name = "root", KVTypeInfo<T>? typeInfo = null)
        {
            using var ms = new MemoryStream();

            if (typeInfo is null)
            {
                serializer.Serialize(ms, value, name);
            }
            else
            {
                serializer.Serialize(ms, value, name, typeInfo);
            }

            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }
}
