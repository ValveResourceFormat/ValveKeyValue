using System.Diagnostics.CodeAnalysis;

namespace ValveKeyValue
{
    interface IObjectReflector
    {
        IEnumerable<IObjectMember> GetMembers([DynamicallyAccessedMembers(Trimming.Properties)] Type objectType, object @object);
    }
}
