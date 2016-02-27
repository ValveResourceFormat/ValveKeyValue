using System;
using System.Reflection;

namespace ValveKeyValue
{
    static class ObjectCopier
    {
        public static void CopyObject<TObject>(KVObject kv, TObject obj)
        {
            CopyObject(kv, obj, new DefaultMapper());
        }

        public static void CopyObject<TObject>(KVObject kv, TObject obj, IPropertyMapper mapper)
        {
            Require.NotNull(kv, nameof(kv));

            // Cannot use Require.NotNull here because TObject might be a struct.
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            Require.NotNull(mapper, nameof(mapper));

            foreach (var item in kv.Items)
            {
                var name = mapper.MapFromKeyValue(item.Name);
                CopyValue(item.Value, obj, name);
            }
        }

        public static void CopyValue<TObject>(KVValue value, TObject obj, string mappedName)
        {
            var property = typeof(TObject).GetProperty(mappedName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null)
            {
                return;
            }

            var propertyType = property.PropertyType;
            if (propertyType == typeof(bool))
            {
                property.SetValue(obj, (bool)value);
            }
            else if (propertyType == typeof(string))
            {
                property.SetValue(obj, (string)value);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
