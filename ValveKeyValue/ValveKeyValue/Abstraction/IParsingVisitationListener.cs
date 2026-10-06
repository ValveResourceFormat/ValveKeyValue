namespace ValveKeyValue.Abstraction
{
    interface IParsingVisitationListener : IVisitationListener
    {
        void DiscardCurrentObject();

        // Removes the first item in the current object whose key matches, ignoring case.
        void RemoveItem(string name);

        IParsingVisitationListener GetMergeListener();

        IParsingVisitationListener GetAppendListener();
    }
}
