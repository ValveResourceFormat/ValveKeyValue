namespace ValveKeyValue.Abstraction
{
    interface IParsingVisitationListener : IVisitationListener
    {
        public string[] StringPool { get; set; }

        void DiscardCurrentObject();

        IParsingVisitationListener GetMergeListener();

        IParsingVisitationListener GetAppendListener();
    }
}
