using System.Xml.Linq;
using BP.IService;
using BP.Models;

namespace BP.Services
{
    public class BrokerStatementsService : IBrokerStatementsService
    {
        private readonly string _xmlFilePath;
        private readonly IEmailService _emailService;

        public BrokerStatementsService(IWebHostEnvironment env, IEmailService emailService)
        {
            _xmlFilePath = Path.Combine(env.ContentRootPath, "Data", "broker-commission-statements.xml");
            _emailService = emailService;
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private XDocument Load()
        {
            if (!File.Exists(_xmlFilePath))
                throw new FileNotFoundException($"Broker commission statements data file not found: {_xmlFilePath}");
            return XDocument.Load(_xmlFilePath);
        }

        private void Save(XDocument doc) => doc.Save(_xmlFilePath);

        private static StatementListItem MapToListItem(XElement e) => new()
        {
            StatementId = (string?)e.Element("StatementId") ?? string.Empty,
            AgencyName = (string?)e.Element("LegalName") ?? string.Empty,
            SubmittedOn = (string?)e.Element("StatementDate") ?? string.Empty,
            Payments = decimal.Parse((string)e.Element("Payments")!),
            Adjustments = decimal.Parse((string)e.Element("Adjustments")!),
            Clawbacks = decimal.Parse((string)e.Element("Clawbacks")!),
            SubTotal = decimal.Parse((string)e.Element("SubTotal")!),
            Amount = decimal.Parse((string)e.Element("Total")!),
            Status = (string?)e.Element("Status") ?? string.Empty,
            PaymentDate = (string?)e.Element("PaymentDate") ?? string.Empty
        };

        private static StatementTransaction MapTransaction(XElement e) => new()
        {
            TransactionId = (string?)e.Element("TransactionId") ?? string.Empty,
            CustomerName = (string?)e.Element("CustomerName") ?? string.Empty,
            UtilityNumber = (string?)e.Element("UtilityNumber") ?? string.Empty,
            ContractNumber = (string?)e.Element("ContractNumber") ?? string.Empty,
            BillDate = (string?)e.Element("BillDate") ?? string.Empty,
            StartDate = (string?)e.Element("StartDate") ?? string.Empty,
            EndDate = (string?)e.Element("EndDate") ?? string.Empty,
            Type = (string?)e.Element("Type") ?? string.Empty,
            Usage = decimal.Parse((string)e.Element("Usage")!),
            ChannelFee = decimal.Parse((string)e.Element("ChannelFee")!),
            BrokerFee = decimal.Parse((string)e.Element("BrokerFee")!),
            UnitFee = decimal.Parse((string)e.Element("UnitFee")!),
            Commission = decimal.Parse((string)e.Element("Commission")!),
            CommodityType = (string?)e.Element("CommodityType") ?? string.Empty
        };

        private static XElement? FindStatement(XDocument doc, string statementId) =>
            doc.Root!
               .Element("Statements")!
               .Elements("Statement")
               .FirstOrDefault(e => (string?)e.Element("StatementId") == statementId);

        // ── Public methods ─────────────────────────────────────────────────────

        public StatementListResponse GetMyStatements(string? agencyId = null)
        {
            var elements = Load()
                .Root!
                .Element("Statements")!
                .Elements("Statement");

            if (!string.IsNullOrWhiteSpace(agencyId))
                elements = elements.Where(e =>
                    string.Equals((string?)e.Element("AgencyId"), agencyId, StringComparison.OrdinalIgnoreCase));

            var items = elements.Select(MapToListItem).ToList();

            return new StatementListResponse
            {
                TotalEntries = items.Count,
                Statements = items
            };
        }

        public StatementDetailResponse? GetStatementDetail(string statementId)
        {
            var statement = FindStatement(Load(), statementId);
            if (statement is null)
                return null;

            var transactions = statement
                .Element("Transactions")?
                .Elements("Transaction")
                .ToList() ?? [];

            return new StatementDetailResponse
            {
                AgencyInfo = new StatementAgencyInfo
                {
                    AgencyId = (string?)statement.Element("AgencyId") ?? string.Empty,
                    LegalName = (string?)statement.Element("LegalName") ?? string.Empty,
                    Contact = (string?)statement.Element("Contact") ?? string.Empty,
                    Email = (string?)statement.Element("Email") ?? string.Empty,
                    PhoneNumber = (string?)statement.Element("PhoneNumber") ?? string.Empty,
                    StatementEmail = (string?)statement.Element("StatementEmail") ?? string.Empty
                },
                StatementSummary = new StatementSummaryInfo
                {
                    StatementId = (string?)statement.Element("StatementId") ?? string.Empty,
                    TotalClawbacks = decimal.Parse((string)statement.Element("Clawbacks")!),
                    TotalWriteOffAmount = decimal.Parse((string)statement.Element("TotalWriteOffAmount")!),
                    StatementDate = (string?)statement.Element("StatementDate") ?? string.Empty,
                    TotalAmountOwed = decimal.Parse((string)statement.Element("TotalAmountOwed")!),
                    TotalAdjustments = decimal.Parse((string)statement.Element("TotalAdjustments")!),
                    Status = (string?)statement.Element("Status") ?? string.Empty,
                    Comments = (string?)statement.Element("Comments") ?? string.Empty
                },
                ForecastedTransactions = transactions
                    .Where(t => string.Equals((string?)t.Element("TransactionState"), "Forecasted", StringComparison.OrdinalIgnoreCase))
                    .Select(MapTransaction)
                    .ToList(),
                CurrentTransactions = transactions
                    .Where(t => string.Equals((string?)t.Element("TransactionState"), "Current", StringComparison.OrdinalIgnoreCase))
                    .Select(MapTransaction)
                    .ToList()
            };
        }

        public StatementActionResponse ApproveStatement(ApproveStatementRequest request)
        {
            var doc = Load();
            var statement = FindStatement(doc, request.StatementId);

            if (statement is null)
                return new StatementActionResponse { Success = false, Message = $"Statement '{request.StatementId}' not found." };

            var currentStatus = (string?)statement.Element("Status") ?? string.Empty;
            if (!string.Equals(currentStatus, "PENDING", StringComparison.OrdinalIgnoreCase))
                return new StatementActionResponse { Success = false, Message = $"Statement '{request.StatementId}' is not in PENDING status." };

            var includedSet = new HashSet<string>(request.IncludedTransactionIds, StringComparer.OrdinalIgnoreCase);

            foreach (var tx in statement.Element("Transactions")?.Elements("Transaction") ?? Enumerable.Empty<XElement>())
            {
                var txId = (string?)tx.Element("TransactionId") ?? string.Empty;
                var stateElement = tx.Element("TransactionState");

                if (stateElement is null) continue;

                stateElement.Value = includedSet.Contains(txId) ? "Current" : "Forecasted";
            }

            statement.Element("Status")!.Value = "APPROVED";

            var commentsElement = statement.Element("Comments");
            if (commentsElement is not null)
                commentsElement.Value = request.Comments ?? string.Empty;

            Save(doc);
            return new StatementActionResponse { Success = true, Message = $"Statement '{request.StatementId}' approved successfully." };
        }

        public StatementActionResponse RejectStatement(RejectStatementRequest request)
        {
            var doc = Load();
            var statement = FindStatement(doc, request.StatementId);

            if (statement is null)
                return new StatementActionResponse { Success = false, Message = $"Statement '{request.StatementId}' not found." };

            var currentStatus = (string?)statement.Element("Status") ?? string.Empty;
            if (!string.Equals(currentStatus, "PENDING", StringComparison.OrdinalIgnoreCase))
                return new StatementActionResponse { Success = false, Message = $"Statement '{request.StatementId}' is not in PENDING status." };

            statement.Element("Status")!.Value = "REJECTED";

            var commentsElement = statement.Element("Comments");
            if (commentsElement is not null)
                commentsElement.Value = request.Comments ?? string.Empty;

            Save(doc);
            return new StatementActionResponse { Success = true, Message = $"Statement '{request.StatementId}' rejected successfully." };
        }

        public StatementActionResponse BulkApprove(BulkStatementRequest request)
        {
            var doc = Load();
            var notFound = new List<string>();
            var notPending = new List<string>();

            foreach (var id in request.StatementIds)
            {
                var statement = FindStatement(doc, id);
                if (statement is null) { notFound.Add(id); continue; }

                var currentStatus = (string?)statement.Element("Status") ?? string.Empty;
                if (!string.Equals(currentStatus, "PENDING", StringComparison.OrdinalIgnoreCase)) { notPending.Add(id); continue; }

                statement.Element("Status")!.Value = "APPROVED";
            }

            // Only write to disk if at least one statement was actually changed
            if (notFound.Count + notPending.Count < request.StatementIds.Count)
                Save(doc);

            if (notFound.Count > 0 || notPending.Count > 0)
            {
                var issues = new List<string>();
                if (notFound.Count > 0) issues.Add($"Not found: {string.Join(", ", notFound)}");
                if (notPending.Count > 0) issues.Add($"Not in PENDING status: {string.Join(", ", notPending)}");
                return new StatementActionResponse { Success = false, Message = string.Join("; ", issues) };
            }

            return new StatementActionResponse { Success = true, Message = $"{request.StatementIds.Count} statement(s) approved successfully." };
        }

        public StatementActionResponse BulkReject(BulkStatementRequest request)
        {
            var doc = Load();
            var notFound = new List<string>();
            var notPending = new List<string>();

            foreach (var id in request.StatementIds)
            {
                var statement = FindStatement(doc, id);
                if (statement is null) { notFound.Add(id); continue; }

                var currentStatus = (string?)statement.Element("Status") ?? string.Empty;
                if (!string.Equals(currentStatus, "PENDING", StringComparison.OrdinalIgnoreCase)) { notPending.Add(id); continue; }

                statement.Element("Status")!.Value = "REJECTED";
            }

            // Only write to disk if at least one statement was actually changed
            if (notFound.Count + notPending.Count < request.StatementIds.Count)
                Save(doc);

            if (notFound.Count > 0 || notPending.Count > 0)
            {
                var issues = new List<string>();
                if (notFound.Count > 0) issues.Add($"Not found: {string.Join(", ", notFound)}");
                if (notPending.Count > 0) issues.Add($"Not in PENDING status: {string.Join(", ", notPending)}");
                return new StatementActionResponse { Success = false, Message = string.Join("; ", issues) };
            }

            return new StatementActionResponse { Success = true, Message = $"{request.StatementIds.Count} statement(s) rejected successfully." };
        }

        public async Task<StatementActionResponse> EmailStatementAsync(EmailStatementRequest request)
        {
            var statement = FindStatement(Load(), request.StatementId);

            if (statement is null)
                return new StatementActionResponse { Success = false, Message = $"Statement '{request.StatementId}' not found." };

            var recipientEmail = (string?)statement.Element("StatementEmail") ?? string.Empty;

            if (string.IsNullOrWhiteSpace(recipientEmail) ||
                string.Equals(recipientEmail, "NA", StringComparison.OrdinalIgnoreCase))
            {
                return new StatementActionResponse { Success = false, Message = "No statement email address is configured for this agency." };
            }
            var status = (string?)statement.Element("Status") ?? string.Empty;
            if (!string.Equals(status, "APPROVED", StringComparison.OrdinalIgnoreCase))
            {
                return new StatementActionResponse { Success = false, Message = $"Statement '{request.StatementId}' is not in APPROVED status." };
            }

            var statementId = (string?)statement.Element("StatementId") ?? string.Empty;
            var legalName = (string?)statement.Element("LegalName") ?? string.Empty;
            var statementDate = (string?)statement.Element("StatementDate") ?? string.Empty;
            var total = decimal.Parse((string)statement.Element("Total")!);
            // var status = (string?)statement.Element("Status") ?? string.Empty;

            var subject = $"Commission Statement #{statementId} — {legalName}";
            var body = $"Dear {legalName},\n\n" +
                       $"Please find below the details for your commission statement.\n\n" +
                       $"Statement ID:   {statementId}\n" +
                       $"Statement Date: {statementDate}\n" +
                       $"Total Amount:   ${total:F2}\n" +
                       $"Status:         {status}\n\n" +
                       $"Please contact us if you have any questions.\n\n" +
                       $"BP Industrial Portal";

            await _emailService.SendAsync(recipientEmail, subject, body);
            return new StatementActionResponse { Success = true, Message = $"Statement #{statementId} emailed to {recipientEmail}." };
        }

        public StatementsSummaryResponse GetStatementsSummary(string? agencyId)
        {
            var elements = Load()
                .Root!
                .Element("Statements")!
                .Elements("Statement");

            if (!string.IsNullOrWhiteSpace(agencyId))
                elements = elements.Where(e =>
                    string.Equals((string?)e.Element("AgencyId"), agencyId, StringComparison.OrdinalIgnoreCase));

            var statements = elements.ToList();

            int statementsSubmitted = statements.Count;
            int pendingCount = statements.Count(e =>
                string.Equals((string?)e.Element("Status"), "PENDING", StringComparison.OrdinalIgnoreCase));
            int approvedStatements = statements.Count(e =>
                string.Equals((string?)e.Element("Status"), "APPROVED", StringComparison.OrdinalIgnoreCase));
            decimal approvedCommission = statements
                .Where(e => string.Equals((string?)e.Element("Status"), "APPROVED", StringComparison.OrdinalIgnoreCase))
                .Sum(e => decimal.Parse((string)e.Element("Total")!));

            return new StatementsSummaryResponse
            {
                StatementsSubmitted = statementsSubmitted,
                PendingCount = pendingCount,
                PendingLabel = $"{pendingCount} pending",
                ApprovedCommission = approvedCommission,
                ApprovedCommissionLabel = "Approved statements total",
                ApprovedStatements = approvedStatements,
                ApprovedStatementsLabel = "Ready for payout"
            };
        }
    }
}
