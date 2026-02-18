namespace Smart_ward_management_system.Model.Common
{
    public class Notice
    {
        public Guid NoticeId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        //public NoticeTypeEnum NoticeType { get; set; }
        public string IssuedBy { get; set; }
        public  DateTime IssuedDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsUrgent { get; set; }
    }
}
