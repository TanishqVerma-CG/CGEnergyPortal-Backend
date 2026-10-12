using System.ComponentModel.DataAnnotations;

namespace BP.Models
{
    public class CaseMessage
    {
        public string MessageId { get; set; } = string.Empty;
        public string CaseId { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }

    public class CaseChatResponse
    {
        public string CaseId { get; set; } = string.Empty;
        public List<CaseMessage> Messages { get; set; } = [];
    }

    public class SendMessageRequest
    {
        [Required]
        public string CaseId { get; set; } = string.Empty;
        [Required]
        public string Text { get; set; } = string.Empty;
    }

    public class SendMessageResponse
    {
        public string MessageId { get; set; } = string.Empty;
        public string CaseId { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }
}
