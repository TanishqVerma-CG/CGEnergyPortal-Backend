using System.Xml.Linq;
using BP.Helpers;
using BP.IService;
using BP.Models;
using Microsoft.AspNetCore.Mvc;

namespace BP.Services
{
    public class SupportCaseService : ISupportCaseService
    {
        private readonly string _xmlFilePath;
        private readonly string _requestTypeFilePath;
        private readonly string _chatFilePath;
        private readonly string _brokerCaseFilePath;
        private readonly NotificationHelper _notificationHelper;

        public SupportCaseService(IWebHostEnvironment env, NotificationHelper notificationHelper)
        {
            _xmlFilePath = Path.Combine(env.ContentRootPath, "Data", "support-cases.xml");
            _requestTypeFilePath = Path.Combine(env.ContentRootPath, "Data", "request-types.xml");
            _chatFilePath = Path.Combine(env.ContentRootPath, "Data", "case-chats.xml");
            _brokerCaseFilePath = Path.Combine(env.ContentRootPath, "Data", "Broker-Support-Case.xml");
            _notificationHelper = notificationHelper;
        }

        private (XDocument doc, List<XElement> caseEls) LoadXmlDoc()
        {
            if (!File.Exists(_xmlFilePath))
                throw new FileNotFoundException($"Support-case data file not found: {_xmlFilePath}");

            var doc = XDocument.Load(_xmlFilePath);
            var caseEls = doc.Root!.Element("Cases")!.Elements("Case").ToList();
            return (doc, caseEls);
        }

        private List<XElement> LoadXml() => LoadXmlDoc().caseEls;

        private static SupportCase MapCase(XElement e) => new()
        {
            CaseId = (string)e.Element("CaseId")!,
            CreatedOn = (string)e.Element("CreatedOn")!,
            ContractId = (string?)e.Element("ContractId") ?? string.Empty,
            Meter = (string)e.Element("Meter")!,
            RequestType = (string)e.Element("RequestType")!,
            Priority = (string)e.Element("Priority")!,
            Subject = (string)e.Element("Subject")!,
            Comments = (string)e.Element("Comments")!,
            Status = (string)e.Element("Status")!,
            ServiceRep = (string)e.Element("ServiceRep")!,
            ClosedDate = (string)e.Element("ClosedDate")!,
            Origin = (string?)e.Element("Origin")?? "Portal"   
        };

        private static SupportCaseSummary CalculateSummary(List<SupportCase> cases)
        {
            var now = DateTime.UtcNow;
            return new SupportCaseSummary
            {
                OpenCases = cases.Count(c => c.Status.Equals("OPEN", StringComparison.OrdinalIgnoreCase)),

                HighPriorityCases = cases.Count(c => c.Priority.Equals("HIGH", StringComparison.OrdinalIgnoreCase) && c.Status.Equals("OPEN", StringComparison.OrdinalIgnoreCase)),

                AwaitingResponse = cases.Count(c => c.Status.Equals("AWAITING RESPONSE", StringComparison.OrdinalIgnoreCase)),

                CasesClosedThisMonth = cases.Count(c => c.Status.Equals("CLOSED", StringComparison.OrdinalIgnoreCase) && DateTime.TryParse(c.CreatedOn, out var d) && d.Year == now.Year && d.Month == now.Month)
            };
        }

        private (XDocument doc, List<XElement> messageEls) LoadChatDoc()
        {
            if (!File.Exists(_chatFilePath))
                throw new FileNotFoundException($"Chat data file not found: {_chatFilePath}");

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

        public SupportCaseListResponse GetAllCases()
        {
            var cases = LoadXml().Select(MapCase).ToList();
            return new SupportCaseListResponse { Summary = CalculateSummary(cases), Cases = cases };
        }

        public SupportCaseDetailResponse? GetCaseById(string caseId)
        {
            var el = LoadXml().FirstOrDefault(e =>
                string.Equals((string)e.Element("CaseId")!, caseId, StringComparison.OrdinalIgnoreCase));

            return el is null ? null : new SupportCaseDetailResponse { Case = MapCase(el) };
        }

        public IActionResult AddSupportRequest(SupportRequest request)
        {
            string newCaseId = CreateCase(request);
            _notificationHelper.SendSupportRequestNotification(newCaseId, request.MeterId);

            string serviceRep = ResolveServiceRep(request.RequestType);
            return new OkObjectResult(new
            {
                message = "Support request submitted successfully.",
                caseId = newCaseId,
                serviceRep
            });
        }

        public string CreateCase(SupportRequest request)
        {
            var (doc, caseEls) = LoadXmlDoc();

            int nextNumber = caseEls
                .Select(e => (string)e.Element("CaseId")!)
                .Where(id => id.StartsWith("CASE-", StringComparison.OrdinalIgnoreCase))
                .Select(id => int.TryParse(id["CASE-".Length..], out var n) ? n : 0)
                .DefaultIfEmpty(0)
                .Max() + 1;

            string newCaseId = $"CASE-{nextNumber}";
            string serviceRep = ResolveServiceRep(request.RequestType);

            doc.Root!.Element("Cases")!.AddFirst(new XElement("Case",
                new XElement("CaseId", newCaseId),
                new XElement("CreatedOn", DateTime.Now.ToString("M/d/yyyy")),
                new XElement("ContractId", request.ContractId),
                new XElement("Meter", request.MeterId),
                new XElement("RequestType", request.RequestType),
                new XElement("Priority", request.Priority.ToUpper()),
                new XElement("Subject", request.Subject),
                new XElement("Comments", request.Comment),
                new XElement("Status", "OPEN"),
                new XElement("Origin", "Portal"),
                new XElement("ServiceRep", serviceRep),
                new XElement("ClosedDate", string.Empty)
            ));

            doc.Save(_xmlFilePath);

            return newCaseId;
        }

        public CaseChatResponse GetCaseChat(string caseId)
        {
            var (_, messageEls) = LoadChatDoc();

            var messages = messageEls
                .Where(e => string.Equals((string)e.Element("CaseId")!, caseId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(e => DateTime.Parse((string)e.Element("SentAt")!))
                .Select(MapMessage)
                .ToList();

            return new CaseChatResponse { CaseId = caseId, Messages = messages };
        }

        public SendMessageResponse SendMessage(SendMessageRequest request)
        {
            var caseEls = LoadXml();
            bool caseExists = caseEls.Any(e =>
                string.Equals((string)e.Element("CaseId")!, request.CaseId, StringComparison.OrdinalIgnoreCase));

            if (!caseExists)
                throw new KeyNotFoundException($"Case '{request.CaseId}' not found.");

            var (doc, messageEls) = LoadChatDoc();

            int nextNumber = messageEls
                .Select(e => (string)e.Element("MessageId")!)
                .Where(id => id.StartsWith("MSG-", StringComparison.OrdinalIgnoreCase))
                .Select(id => int.TryParse(id["MSG-".Length..], out var n) ? n : 0)
                .DefaultIfEmpty(0)
                .Max() + 1;

            string newMessageId = $"MSG-{nextNumber:D3}";
            var sentAt = DateTime.Now;

            doc.Root!.Element("Messages")!.Add(new XElement("Message",
                new XElement("MessageId", newMessageId),
                new XElement("CaseId", request.CaseId),
                new XElement("Sender", "Customer"),
                new XElement("SenderName", "Customer"),
                new XElement("Text", request.Text),
                new XElement("SentAt", sentAt.ToString("yyyy-MM-ddTHH:mm:ss"))
            ));

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

        private string ResolveServiceRep(string requestType)
        {
            if (!File.Exists(_requestTypeFilePath)) return string.Empty;

            var doc = XDocument.Load(_requestTypeFilePath);
            var match = doc.Root!.Element("RequestTypes")!.Elements("RequestType")
                .FirstOrDefault(e => string.Equals(
                    ((string)e.Element("Label")!).Trim(),
                    requestType.Trim(),
                    StringComparison.OrdinalIgnoreCase));

            return match is null ? string.Empty : (string)match.Element("ServiceRep")!;
        }

        public RequestTypeListResponse GetRequestTypes()
        {
            if (!File.Exists(_requestTypeFilePath))
                throw new FileNotFoundException($"Request-type data file not found: {_requestTypeFilePath}");

            var doc = XDocument.Load(_requestTypeFilePath);
            var items = doc.Root!.Element("RequestTypes")!.Elements("RequestType")
                .Select(e => new RequestTypeItem
                {
                    Label = ((string)e.Element("Label")!).Trim(),
                    ServiceRep = ((string)e.Element("ServiceRep")!).Trim()
                }).ToList();

            return new RequestTypeListResponse { RequestTypes = items };
        }

        public IActionResult AddBrokerSupportRequest(SupportRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CustomerName))
                return new BadRequestObjectResult(new { message = "CustomerName is required for broker support requests." });

            if (!File.Exists(_brokerCaseFilePath))
                throw new FileNotFoundException($"Broker support case data file not found: {_brokerCaseFilePath}");

            var doc = XDocument.Load(_brokerCaseFilePath);

            var existingCases = doc.Root!.Elements("Case").ToList();

            int nextNumber = existingCases.Select(e => (string?)e.Element("CaseNumber") ?? string.Empty).Where(id => id.StartsWith("CID-", StringComparison.OrdinalIgnoreCase)).Select(id => int.TryParse(id["CID-".Length..], out var n) ? n :0).DefaultIfEmpty(0).Max() +1;

            string newCaseNumber = $"CID-{nextNumber}";
            string today = DateTime.Now.ToString("M/d/yyyy");
            string serviceRep = ResolveServiceRep(request.RequestType);

            var newCase = new XElement("Case",
                new XElement("Customer", request.CustomerName),
                request.CustomerId >0 ? new XElement("CustomerId", request.CustomerId) : null,
                new XElement("CaseNumber", newCaseNumber),
                new XElement("RequestType", request.RequestType),
                !string.IsNullOrWhiteSpace(request.ContractId) ? new XElement("ContractId", request.ContractId) : new XElement("ContractId", string.Empty),
                !string.IsNullOrWhiteSpace(request.MeterId) ? new XElement("Meter", request.MeterId) : new XElement("Meter", string.Empty),
                new XElement("ServiceRep", serviceRep ?? string.Empty),
                new XElement("Origin", "Portal"),
                new XElement("Priority", request.Priority.ToUpper()),
                new XElement("CreatedOn", today),
                new XElement("ClosedOn", "--"),
                new XElement("Status", "OPEN"),
                new XElement("LastActivity", today),
                new XElement("RaisedBy", "Broker"),
                new XElement("Comment", request.Comment),
                new XElement("Subject", request.Subject)
            );

            doc.Root.AddFirst(newCase);
            doc.Save(_brokerCaseFilePath);
            _notificationHelper.SendSupportRequestNotification(newCaseNumber, request.MeterId);
            return new OkObjectResult(new
            {
                message = "Broker support request submitted successfully.",
                caseNumber = newCaseNumber,
                customer = request.CustomerName
            });
        }
    }
}
