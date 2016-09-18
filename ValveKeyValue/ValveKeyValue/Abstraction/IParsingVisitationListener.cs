namespace ValveKeyValue.Abstraction
{
    interface IParsingVisitationListener : IVisitationListener
    {
        void DiscardCurrentObject();

        IParsingVisitationListener GetMergeListener();

        IParsingVisitationListener GetAppendListener();
    }
}
