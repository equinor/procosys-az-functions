namespace ProCoSys.IndexUpdate.IndexModel
{
    class PunchItem
    {
        public string PunchItemNo { get; set; }
        public string Description { get; set; }
        public string TagNo { get; set; }
        public string Responsible { get; set; }
        public string FormType { get; set; }
        public string Category { get; set; }
        public const string TopicName = "punchlistitem";
    }
}
