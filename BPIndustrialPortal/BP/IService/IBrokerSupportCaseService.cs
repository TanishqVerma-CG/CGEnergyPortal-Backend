using BP.Models;

namespace BP.IService
{
    public interface IBrokerSupportCaseService
    {
        BrokerSupportCaseSummary GetSummary();
        BrokerSupportCaseListResponse GetCases(string? raisedBy = null, int page = 1, int pageSize = 10, string? search = null);
        BrokerSupportCase? GetCaseById(string caseNumber);
        CaseChatResponse GetCaseChat(string caseNumber);
        SendMessageResponse SendMessage(SendMessageRequest request);
    }
}
