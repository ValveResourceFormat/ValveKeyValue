using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ValveKeyValue
{
    interface IObjectReflector
    {
        IEnumerable<IObjectMember> GetMembers(
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(Trimming.Properties)]
#endif
            Type type,
            object @object);
    }
}
