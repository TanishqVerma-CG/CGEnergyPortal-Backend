using BP.Models;
using Microsoft.AspNetCore.Mvc;

namespace BP.IService
{
    public interface ISupportCaseService
    {
        IActionResult AddSupportRequest(SupportRequest request);
        IActionResult AddBrokerSupportRequest(SupportRequest request);
        string CreateCase(SupportRequest request);
        SupportCaseListResponse GetAllCases();
        SupportCaseDetailResponse? GetCaseById(string caseId);
        RequestTypeListResponse GetRequestTypes();
        CaseChatResponse GetCaseChat(string caseId);
        SendMessageResponse SendMessage(SendMessageRequest request);
    }
}
