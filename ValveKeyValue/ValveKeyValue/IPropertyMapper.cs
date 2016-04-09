using System.Collections.Generic;

namespace ValveKeyValue
{
    interface IPropertyMapper
    {
        IEnumerable<IObjectMember> GetMembers(object @object);
    }
}
