namespace ValveKeyValue
{
    interface IObjectReflector
    {
        IEnumerable<IObjectMember> GetMembers(object @object);
    }
}
