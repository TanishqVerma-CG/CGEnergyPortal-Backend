using System.ComponentModel.DataAnnotations;

namespace BP.Models
{
    public class BillingSummary
    {
        public decimal CurrentBalance { get; set; }
        public string InvoicedAmountStatus { get; set; } = string.Empty;
        public int OpenInvoices { get; set; }
        public int OverdueCount { get; set; }
    }

    // ── Outstanding Invoices ──────────────────────────────────────────────
    public class OutstandingInvoice
    {
        public string InvoiceId { get; set; } = string.Empty;
        public string ContractId { get; set; } = string.Empty;
        public decimal TotalDue { get; set; }
        public string DueDate { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class OutstandingInvoicesResponse
    {
        public int TotalEntries { get; set; }
        public List<OutstandingInvoice> Invoices { get; set; } = [];
    }

    // ── Invoice History ───────────────────────────────────────────────────
    public class InvoiceHistoryItem
    {
        public string InvoiceId { get; set; } = string.Empty;
        public string ContractId { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public decimal TotalDue { get; set; }
        public decimal PastDue { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class InvoiceHistoryResponse
    {
        public int TotalEntries { get; set; }
        public List<InvoiceHistoryItem> Invoices { get; set; } = [];
    }

    // ── Payment History ───────────────────────────────────────────────────
    public class PaymentHistoryItem
    {
        public string PaymentId { get; set; } = string.Empty;
        public string InvoiceId { get; set; } = string.Empty;
        public string ContractId { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class PaymentHistoryResponse
    {
        public int TotalEntries { get; set; }
        public List<PaymentHistoryItem> Payments { get; set; } = [];
    }

    // ── Invoice Detail ────────────────────────────────────────────────────
    public class InvoiceChargeBreakdown
    {
        public decimal EnergyCharge { get; set; }
        public decimal CapacityCharge { get; set; }
        public decimal RenewableEnergyCharge { get; set; }
        public decimal Losses { get; set; }
        public decimal TransmissionCharge { get; set; }
        public decimal Taxes { get; set; }
        public decimal Total { get; set; }
    }

    public class InvoiceMeterCharge
    {
        public string MeterId { get; set; } = string.Empty;
        public decimal UsageMWh { get; set; }
        public decimal Amount { get; set; }
    }

    public class InvoiceDetail
    {
        public string InvoiceId { get; set; } = string.Empty;
        public string ContractId { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public InvoiceChargeBreakdown ChargeBreakdown { get; set; } = new();
        public List<InvoiceMeterCharge> MeterCharges { get; set; } = [];
        public decimal TotalMeterCharges { get; set; }
    }

    public class MakePaymentRequest
    {
        [Required]
        public string InvoiceId { get; set; } = string.Empty;
        [Required]
        public string ContractId { get; set; } = string.Empty;
        [Required]
        public string AccountNumber { get; set; } = string.Empty;
        [Required]
        public string RoutingNumber { get; set; } = string.Empty;
    }

    public class MakePaymentResponse
    {
        public string PaymentId { get; set; } = string.Empty;
        public string InvoiceId { get; set; } = string.Empty;
        public string ContractId { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
