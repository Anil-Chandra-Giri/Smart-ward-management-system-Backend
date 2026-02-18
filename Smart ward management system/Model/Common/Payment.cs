namespace Smart_ward_management_system.Model.Common
{
    public class Payment
    {
        public Guid PaymentId { get; set; }
        public Guid ReferenceId { get; set; }
        public string ReferenceType { get; set; }
        public int Amount { get; set; }
        public string PaymentGateway { get; set; }
        public Guid TransactionId { get; set; }
        public bool PaymentStatus { get; set; }
        public DateTime PaidAt { get; set; }

    }
}
