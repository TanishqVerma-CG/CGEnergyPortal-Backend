using System.Xml.Linq;
using BP.IService;
using BP.Models;
using ClosedXML.Excel;

namespace BP.Services
{
    public class BrokerDealsService : IBrokerDealsService
    {
        private readonly string _xmlFilePath;

        public BrokerDealsService(IWebHostEnvironment env)
        {
            _xmlFilePath = Path.Combine(env.ContentRootPath, "Data", "broker-commission-deals.xml");
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private XDocument Load()
        {
            if (!File.Exists(_xmlFilePath))
                throw new FileNotFoundException($"Broker commission deals data file not found: {_xmlFilePath}");
            return XDocument.Load(_xmlFilePath);
        }

        private static DealItem MapDeal(XElement e) => new()
        {
            CompanyName = (string?)e.Element("CompanyName") ?? string.Empty,
            ContractNumber = (string?)e.Element("ContractNumber") ?? string.Empty,
            ContractStartDate = (string?)e.Element("ContractStartDate") ?? string.Empty,
            Term = int.Parse((string)e.Element("Term")!),
            CommissionPlan = (string?)e.Element("CommissionPlan") ?? string.Empty,
            TermVolume = decimal.Parse((string)e.Element("TermVolume")!),
            UnitFee = decimal.Parse((string)e.Element("UnitFee")!),
            CurrentPaymentDate = (string?)e.Element("CurrentPaymentDate") ?? string.Empty,
            CurrentPayment = decimal.Parse((string)e.Element("CurrentPayment")!)
        };

        // ── Public methods ─────────────────────────────────────────────────────

        public DealsResponse GetMyDeals(string? agencyId = null)
        {
            var root = Load().Root!;
            var elements = root.Element("Deals")!.Elements("Deal");

            if (!string.IsNullOrWhiteSpace(agencyId))
                elements = elements.Where(e =>
                    string.Equals((string?)e.Element("AgencyId"), agencyId, StringComparison.OrdinalIgnoreCase));

            var deals = elements.Select(MapDeal).ToList();

            return new DealsResponse
            {
                TotalEntries = deals.Count,
                Deals = deals
            };
        }

        public byte[] ExportDeals(string? agencyId = null)
        {
            var deals = GetMyDeals(agencyId).Deals;

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("My Deals");

            // Header row
            ws.Cell(1, 1).Value = "Company Name";
            ws.Cell(1, 2).Value = "Contract Number";
            ws.Cell(1, 3).Value = "Contract Start Date";
            ws.Cell(1, 4).Value = "Term (Months)";
            ws.Cell(1, 5).Value = "Commission Plan";
            ws.Cell(1, 6).Value = "Term Volume";
            ws.Cell(1, 7).Value = "Unit Fee";
            ws.Cell(1, 8).Value = "Current Payment Date";
            ws.Cell(1, 9).Value = "Current Payment";

            var headerRow = ws.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            headerRow.Style.Font.FontColor = XLColor.White;

            // Data rows
            for (int i = 0; i < deals.Count; i++)
            {
                var d = deals[i];
                int row = i + 2;
                ws.Cell(row, 1).Value = d.CompanyName;
                ws.Cell(row, 2).Value = d.ContractNumber;
                ws.Cell(row, 3).Value = d.ContractStartDate;
                ws.Cell(row, 4).Value = d.Term;
                ws.Cell(row, 5).Value = d.CommissionPlan;
                ws.Cell(row, 6).Value = d.TermVolume;
                ws.Cell(row, 7).Value = d.UnitFee;
                ws.Cell(row, 8).Value = d.CurrentPaymentDate;
                ws.Cell(row, 9).Value = d.CurrentPayment;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
