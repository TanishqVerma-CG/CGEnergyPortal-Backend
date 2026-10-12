using System.Xml.Linq;
using BP.IService;
using BP.Models;

namespace BP.Services
{
    public class BrokerSupportCaseService : IBrokerSupportCaseService
    {
        private readonly string _xmlFilePath;
        private readonly string _chatFilePath;

        public BrokerSupportCaseService(IWebHostEnvironment env)
        {
            _xmlFilePath = Path.Combine(env.ContentRootPath, "Data", "Broker-Support-Case.xml");
            _chatFilePath = Path.Combine(env.ContentRootPath, "Data", "broker-case-chats.xml");
        }

        private List<XElement> LoadXml()
        {
            if (!File.Exists(_xmlFilePath))
                throw new FileNotFoundException($"Broker support case data file not found: {_xmlFilePath}");

            var doc = XDocument.Load(_xmlFilePath);
            return doc.Root!.Elements("Case").ToList();
        }

        private static BrokerSupportCase MapCase(XElement e) => new()
        {
            CaseNumber = (string?)e.Element("CaseNumber") ?? string.Empty,
            CustomerId = (int?)e.Element("CustomerId") ??0,
            Customer = (string?)e.Element("Customer") ?? string.Empty,
            RequestType = (string?)e.Element("RequestType") ?? string.Empty,
            ContractId = (string?)e.Element("ContractId") ?? string.Empty,
            Meter = (string?)e.Element("Meter") ?? string.Empty,
            ServiceRep = (string?)e.Element("ServiceRep") ?? string.Empty,
            Origin = (string?)e.Element("Origin") ?? string.Empty,
            Priority = (string?)e.Element("Priority") ?? string.Empty,
            CreatedOn = (string?)e.Element("CreatedOn") ?? string.Empty,
            ClosedOn = (string?)e.Element("ClosedOn") ?? string.Empty,
            Status = (string?)e.Element("Status") ?? string.Empty,
            LastActivity = (string?)e.Element("LastActivity") ?? string.Empty,
            RaisedBy = (string?)e.Element("RaisedBy") ?? string.Empty,
            Comment = (string?)e.Element("Comment") ?? string.Empty,
            Subject = (string?)e.Element("Subject") ?? string.Empty
        };

        private static BrokerSupportCaseSummary CalculateSummary(List<BrokerSupportCase> cases)
        {
            var now = DateTime.UtcNow;
            return new BrokerSupportCaseSummary
            {
                OpenCases = cases.Count(c => c.Status.Equals("OPEN", StringComparison.OrdinalIgnoreCase)),

                HighPriorityCases = cases.Count(c =>
                   c.Priority.Equals("HIGH", StringComparison.OrdinalIgnoreCase) &&
              c.Status.Equals("OPEN", StringComparison.OrdinalIgnoreCase)),

                AwaitingResponse = cases.Count(c =>
                   c.Status.Equals("AWAITING RESPONSE", StringComparison.OrdinalIgnoreCase)),

                CasesClosedThisMonth = cases.Count(c =>
                  c.Status.Equals("CLOSED", StringComparison.OrdinalIgnoreCase) &&
               DateTime.TryParse(c.ClosedOn, out var d) &&
              d.Year == now.Year && d.Month == now.Month)
            };
        }

        public BrokerSupportCaseSummary GetSummary()
        {
            var cases = LoadXml().Select(MapCase).ToList();
            return CalculateSummary(cases);
        }

        public BrokerSupportCaseListResponse GetCases(string? raisedBy = null, int page = 1, int pageSize = 10, string? search = null)
        {
            var cases = LoadXml().Select(MapCase).AsEnumerable();

            if (!string.IsNullOrWhiteSpace(raisedBy))
                cases = cases.Where(c => c.RaisedBy.Equals(raisedBy, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(search))
                cases = cases.Where(c =>
                    c.Customer.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    c.CaseNumber.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    c.RequestType.Contains(search, StringComparison.OrdinalIgnoreCase));

            var filteredCases = cases
                .OrderByDescending(c => c.CreatedOn)
                .ThenBy(c => c.CaseNumber)
                .ToList();
            var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
            var totalPages = Math.Max(1, (int)Math.Ceiling(filteredCases.Count / (double)normalizedPageSize));
            var normalizedPage = Math.Clamp(page, 1, totalPages);

            return new BrokerSupportCaseListResponse
            {
                Cases = filteredCases
                    .Skip((normalizedPage - 1) * normalizedPageSize)
                    .Take(normalizedPageSize)
                    .ToList(),
                TotalEntries = filteredCases.Count,
                CurrentPage = normalizedPage,
                PageSize = normalizedPageSize,
                TotalPages = totalPages
            };
        }

        public BrokerSupportCase? GetCaseById(string caseNumber)
        {
            var el = LoadXml().FirstOrDefault(e =>
        string.Equals((string?)e.Element("CaseNumber"), caseNumber, StringComparison.OrdinalIgnoreCase));

            return el is null ? null : MapCase(el);
        }

        private (XDocument doc, List<XElement> messageEls) LoadChatDoc()
        {
            if (!File.Exists(_chatFilePath))
                throw new FileNotFoundException($"Broker chat data file not found: {_chatFilePath}");

            var doc = XDocument.Load(_chatFilePath);
            var messageEls = doc.Root!.Element("Messages")!.Elements("Message").ToList();
            return (doc, messageEls);
        }

        private static CaseMessage MapMessage(XElement e) => new()
        {
            MessageId = (string)e.Element("MessageId")!,
            CaseId = (string)e.Element("CaseId")!,
            Sender = (string)e.Element("Sender")!,
            SenderName = (string)e.Element("SenderName")!,
            Text = (string)e.Element("Text")!,
            SentAt = DateTime.Parse((string)e.Element("SentAt")!)
        };

        public CaseChatResponse GetCaseChat(string caseNumber)
        {
            var (_, messageEls) = LoadChatDoc();

            var messages = messageEls.Where(e => string.Equals((string)e.Element("CaseId")!, caseNumber, StringComparison.OrdinalIgnoreCase)).OrderBy(e => DateTime.Parse((string)e.Element("SentAt")!)).Select(MapMessage).ToList();

            return new CaseChatResponse { CaseId = caseNumber, Messages = messages };
        }

        public SendMessageResponse SendMessage(SendMessageRequest request)
        {
            var caseEls = LoadXml();
            bool caseExists = caseEls.Any(e =>string.Equals((string?)e.Element("CaseNumber"), request.CaseId, StringComparison.OrdinalIgnoreCase));

            if (!caseExists)
                throw new KeyNotFoundException($"Case '{request.CaseId}' not found.");

            var (doc, messageEls) = LoadChatDoc();

            int nextNumber = messageEls
                .Select(e => (string)e.Element("MessageId")!).Where(id => id.StartsWith("BMSG-", StringComparison.OrdinalIgnoreCase)).Select(id => int.TryParse(id["BMSG-".Length..], out var n) ? n : 0).DefaultIfEmpty(0).Max() + 1;

            string newMessageId = $"BMSG-{nextNumber:D3}";
            var sentAt = DateTime.Now;

            doc.Root!.Element("Messages")!.Add(new XElement("Message",
            new XElement("MessageId", newMessageId),
            new XElement("CaseId", request.CaseId),
            new XElement("Sender", "Customer"),
            new XElement("SenderName", "Customer"),
            new XElement("Text", request.Text),
            new XElement("SentAt", sentAt.ToString("yyyy-MM-ddTHH:mm:ss"))));

            doc.Save(_chatFilePath);

            return new SendMessageResponse
            {
                MessageId = newMessageId,
                CaseId = request.CaseId,
                Sender = "Customer",
                SenderName = "Customer",
                Text = request.Text,
                SentAt = sentAt
            };
        }
    }
}
