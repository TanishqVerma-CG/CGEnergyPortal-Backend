namespace BP.Models
{
    public class ContractBillingSummary
    {
        public string ContractId { get; set; } = string.Empty;
        public string InvoiceId { get; set; } = string.Empty;
        public string BillingPeriod { get; set; } = string.Empty;
        public decimal InvoicedAmount { get; set; }
        public string InvoicedAmountStatus { get; set; } = string.Empty;
        public string DueDate { get; set; } = string.Empty;
        public string AutoPayStatus { get; set; } = string.Empty;
        public string AutoPayScheduledDate { get; set; } = string.Empty;
    }
}
