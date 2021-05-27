namespace ProCoSys.IndexUpdate.IndexModel
{
    class Tag
    {
        public string TagNo { get; set; }
        public string Description { get; set; }
        public string McPkgNo { get; set; }
        public string CommPkgNo { get; set; }
        public string Area { get; set; }
        public string DisciplineCode { get; set; }
        public string DisciplineDescription { get; set; }
        public string CallOffNo { get; set; }
        public string PurchaseOrderNo { get; set; }
        public string TagFunctionCode { get; set; }
        public const string TopicName = "tag";
    }
}
