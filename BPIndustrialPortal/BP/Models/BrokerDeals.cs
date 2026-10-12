namespace BP.Models
{
    // ── Deals ──────────────────────────────────────────────────────────────────

    public class DealItem
    {
        public string CompanyName { get; set; } = string.Empty;
        public string ContractNumber { get; set; } = string.Empty;
        public string ContractStartDate { get; set; } = string.Empty;
        public int Term { get; set; }
        public string CommissionPlan { get; set; } = string.Empty;
        public decimal TermVolume { get; set; }
        public decimal UnitFee { get; set; }
        public string CurrentPaymentDate { get; set; } = string.Empty;
        public decimal CurrentPayment { get; set; }
    }

    public class DealsResponse
    {
        public int TotalEntries { get; set; }
        public List<DealItem> Deals { get; set; } = [];
    }
}
