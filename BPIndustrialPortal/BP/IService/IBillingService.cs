using BP.Models;

namespace BP.IService
{
    public interface IBillingService
    {
        BillingSummary GetSummary();
        OutstandingInvoicesResponse GetOutstandingInvoices();
        InvoiceHistoryResponse GetInvoiceHistory();
        PaymentHistoryResponse GetPaymentHistory();
        InvoiceDetail? GetInvoiceDetail(string invoiceId);
        MakePaymentResponse MakePayment(MakePaymentRequest request);
        ContractBillingSummary? GetContractBillingSummary(string contractId);
    }
}
