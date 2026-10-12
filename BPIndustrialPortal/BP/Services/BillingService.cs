using System.Xml.Linq;
using BP.IService;
using BP.Models;

namespace BP.Services
{
    public class BillingService : IBillingService
    {
        private readonly string _xmlFilePath;

        public BillingService(IWebHostEnvironment env)
        {
            _xmlFilePath = Path.Combine(env.ContentRootPath, "Data", "billing.xml");
        }

        private XDocument Load()
        {
            if (!File.Exists(_xmlFilePath))
                throw new FileNotFoundException($"Billing data file not found: {_xmlFilePath}");
            return XDocument.Load(_xmlFilePath);
        }

        public BillingSummary GetSummary()
        {
            var s = Load().Root!.Element("Summary")!;
            return new BillingSummary
            {
                CurrentBalance = decimal.Parse((string)s.Element("CurrentBalance")!),
                InvoicedAmountStatus = (string?)s.Element("InvoicedAmountStatus") ?? string.Empty,
                OpenInvoices = int.Parse((string)s.Element("OpenInvoices")!),
                OverdueCount = int.Parse((string)s.Element("OverdueCount")!)
            };
        }

        public OutstandingInvoicesResponse GetOutstandingInvoices()
        {
            var root = Load().Root!.Element("OutstandingInvoices")!;
            var invoices = root.Elements("Invoice")
            .Select(e => new OutstandingInvoice
            {
                InvoiceId = (string?)e.Element("InvoiceId") ?? string.Empty,
                ContractId = (string?)e.Element("ContractId") ?? string.Empty,
                TotalDue = decimal.Parse((string)e.Element("TotalDue")!),
                DueDate = (string?)e.Element("DueDate") ?? string.Empty,
                Status = (string?)e.Element("Status") ?? string.Empty
            }).ToList();

            return new OutstandingInvoicesResponse
            {
                TotalEntries = int.Parse((string)root.Element("TotalEntries")!),
                Invoices = invoices
            };
        }

        public InvoiceHistoryResponse GetInvoiceHistory()
        {
            var root = Load().Root!.Element("InvoiceHistory")!;
            var invoices = root.Elements("Invoice")
             .Select(e => new InvoiceHistoryItem
             {
                 InvoiceId = (string?)e.Element("InvoiceId") ?? string.Empty,
                 ContractId = (string?)e.Element("ContractId") ?? string.Empty,
                 Date = (string?)e.Element("Date") ?? string.Empty,
                 TotalDue = decimal.Parse((string)e.Element("TotalDue")!),
                 PastDue = decimal.Parse((string)e.Element("PastDue")!),
                 Status = (string?)e.Element("Status") ?? string.Empty
             }).ToList();

            return new InvoiceHistoryResponse
            {
                TotalEntries = int.Parse((string)root.Element("TotalEntries")!),
                Invoices = invoices
            };
        }

        public PaymentHistoryResponse GetPaymentHistory()
        {
            var root = Load().Root!.Element("PaymentHistory")!;
            var payments = root.Elements("Payment").Select(e => new PaymentHistoryItem
            {
                PaymentId = (string?)e.Element("PaymentId") ?? string.Empty,
                InvoiceId = (string?)e.Element("InvoiceId") ?? string.Empty,
                ContractId = (string?)e.Element("ContractId") ?? string.Empty,
                Date = (string?)e.Element("Date") ?? string.Empty,
                Type = (string?)e.Element("Type") ?? string.Empty,
                Amount = decimal.Parse((string)e.Element("Amount")!)
            }).ToList();

            return new PaymentHistoryResponse
            {
                TotalEntries = int.Parse((string)root.Element("TotalEntries")!),
                Payments = payments
            };
        }

        public InvoiceDetail? GetInvoiceDetail(string invoiceId)
        {
            var root = Load().Root!.Element("InvoiceDetails");
            if (root is null) return null;

            var e = root.Elements("InvoiceDetail").FirstOrDefault(x => string.Equals((string?)x.Element("InvoiceId"), invoiceId, StringComparison.OrdinalIgnoreCase));
            if (e is null) return null;

            var cb = e.Element("ChargeBreakdown")!;
            var mc = e.Element("MeterCharges")!;

            return new InvoiceDetail
            {
                InvoiceId = (string?)e.Element("InvoiceId") ?? string.Empty,
                ContractId = (string?)e.Element("ContractId") ?? string.Empty,
                Date = (string?)e.Element("Date") ?? string.Empty,
                ChargeBreakdown = new InvoiceChargeBreakdown
                {
                    EnergyCharge = decimal.Parse((string)cb.Element("EnergyCharge")!),
                    CapacityCharge = decimal.Parse((string)cb.Element("CapacityCharge")!),
                    RenewableEnergyCharge = decimal.Parse((string)cb.Element("RenewableEnergyCharge")!),
                    Losses = decimal.Parse((string)cb.Element("Losses")!),
                    TransmissionCharge = decimal.Parse((string)cb.Element("TransmissionCharge")!),
                    Taxes = decimal.Parse((string)cb.Element("Taxes")!),
                    Total = decimal.Parse((string)cb.Element("Total")!)
                },
                MeterCharges = mc.Elements("Meter").Select(m => new InvoiceMeterCharge
                {
                    MeterId = (string?)m.Element("MeterId") ?? string.Empty,
                    UsageMWh = decimal.Parse((string)m.Element("UsageMWh")!),
                    Amount = decimal.Parse((string)m.Element("Amount")!)
                }).ToList(),
                TotalMeterCharges = decimal.Parse((string)mc.Element("TotalMeterCharges")!)
            };
        }

        public MakePaymentResponse MakePayment(MakePaymentRequest request)
        {
            if (!File.Exists(_xmlFilePath))
                throw new FileNotFoundException($"Billing data file not found: {_xmlFilePath}");

            var doc = XDocument.Load(_xmlFilePath);
            var root = doc.Root!;

            var outstandingRoot = root.Element("OutstandingInvoices")!;
            var outstandingInvoice = outstandingRoot.Elements("Invoice").FirstOrDefault(e => string.Equals((string?)e.Element("InvoiceId"), request.InvoiceId, StringComparison.OrdinalIgnoreCase) && string.Equals((string?)e.Element("ContractId"), request.ContractId, StringComparison.OrdinalIgnoreCase));

            if (outstandingInvoice is null)
                throw new KeyNotFoundException($"Invoice '{request.InvoiceId}' not found in outstanding invoices.");

            decimal amount = decimal.Parse((string)outstandingInvoice.Element("TotalDue")!);

            var paymentRoot = root.Element("PaymentHistory")!;
            var existingIds = paymentRoot.Elements("Payment").Select(p => (string?)p.Element("PaymentId") ?? string.Empty).Where(id => id.StartsWith("PAY", StringComparison.OrdinalIgnoreCase)).Select(id => int.TryParse(id[3..], out var n) ? n : 0).ToList();
            int nextNum = (existingIds.Count > 0 ? existingIds.Max() : 0) + 1;
            string newPaymentId = $"PAY{nextNum:D2}";

            string paymentDate = DateTime.UtcNow.ToString("M/d/yyyy");

            var newPayment = new XElement("Payment",
            new XElement("PaymentId", newPaymentId),
            new XElement("InvoiceId", request.InvoiceId),
            new XElement("ContractId", request.ContractId),
            new XElement("Date", paymentDate),
            new XElement("Type", "ACH"),
            new XElement("Amount", amount.ToString("F2")));
            paymentRoot.Add(newPayment);

            var payTotalEl = paymentRoot.Element("TotalEntries")!;
            payTotalEl.Value = (int.Parse(payTotalEl.Value) + 1).ToString();

            outstandingInvoice.Remove();
            var outTotalEl = outstandingRoot.Element("TotalEntries")!;
            int newOutTotal = int.Parse(outTotalEl.Value) - 1;
            outTotalEl.Value = newOutTotal.ToString();

            var historyRoot = root.Element("InvoiceHistory")!;
            var historyInvoice = historyRoot.Elements("Invoice").FirstOrDefault(e => string.Equals((string?)e.Element("InvoiceId"), request.InvoiceId, StringComparison.OrdinalIgnoreCase));
            if (historyInvoice is not null)
            {
                var statusEl = historyInvoice.Element("Status");
                if (statusEl is not null) statusEl.Value = "PAID";
                else historyInvoice.Add(new XElement("Status", "PAID"));

                var pastDueEl = historyInvoice.Element("PastDue");
                if (pastDueEl is not null) pastDueEl.Value = "0.00";
            }

            var summaryEl = root.Element("Summary")!;

            var remainingOutstanding = outstandingRoot.Elements("Invoice").ToList();
            int openCount = remainingOutstanding.Count;
            int overdueCount = remainingOutstanding.Count(e => string.Equals((string?)e.Element("Status"), "OVERDUE", StringComparison.OrdinalIgnoreCase));

            decimal newBalance = remainingOutstanding
                     .Sum(e => decimal.TryParse((string?)e.Element("TotalDue"), out var d) ? d : 0m);

            summaryEl.Element("CurrentBalance")!.Value = newBalance.ToString("F2");
            summaryEl.Element("OpenInvoices")!.Value = openCount.ToString();
            summaryEl.Element("OverdueCount")!.Value = overdueCount.ToString();
            summaryEl.Element("InvoicedAmountStatus")!.Value = overdueCount > 0 ? "Overdue" : (openCount > 0 ? "Due" : "Paid");

            doc.Save(_xmlFilePath);

            return new MakePaymentResponse
            {
                PaymentId = newPaymentId,
                InvoiceId = request.InvoiceId,
                ContractId = request.ContractId,
                Date = paymentDate,
                Type = "ACH",
                Amount = amount,
                Message = $"Payment {newPaymentId} for invoice '{request.InvoiceId}' processed successfully."
            };
        }

        public ContractBillingSummary? GetContractBillingSummary(string contractId)
        {
            var doc = Load();
            var element = doc.Root!
                .Element("ContractBillingSummaries")!
                .Elements("ContractBillingSummary")
                .FirstOrDefault(e => ((string?)e.Element("ContractId") ?? string.Empty)
                    .Equals(contractId, StringComparison.OrdinalIgnoreCase));

            if (element is null) return null;

            return new ContractBillingSummary
            {
                ContractId           = (string?)element.Element("ContractId") ?? string.Empty,
                InvoiceId            = (string?)element.Element("InvoiceId") ?? string.Empty,
                BillingPeriod        = (string?)element.Element("BillingPeriod") ?? string.Empty,
                InvoicedAmount       = decimal.Parse((string)element.Element("InvoicedAmount")!),
                InvoicedAmountStatus = (string?)element.Element("InvoicedAmountStatus") ?? string.Empty,
                DueDate              = (string?)element.Element("DueDate") ?? string.Empty,
                AutoPayStatus        = (string?)element.Element("AutoPayStatus") ?? string.Empty,
                AutoPayScheduledDate = (string?)element.Element("AutoPayScheduledDate") ?? string.Empty
            };
        }
    }
}
