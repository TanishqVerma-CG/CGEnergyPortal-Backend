using BP.Models;

namespace BP.IService
{
    public interface IBrokerStatementsService
    {
        StatementListResponse GetMyStatements(string? agencyId = null);
        StatementDetailResponse? GetStatementDetail(string statementId);
        StatementActionResponse ApproveStatement(ApproveStatementRequest request);
        StatementActionResponse RejectStatement(RejectStatementRequest request);
        StatementActionResponse BulkApprove(BulkStatementRequest request);
        StatementActionResponse BulkReject(BulkStatementRequest request);
        Task<StatementActionResponse> EmailStatementAsync(EmailStatementRequest request);
        StatementsSummaryResponse GetStatementsSummary(string? agencyId);
    }
}
