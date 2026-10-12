namespace BP.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string MeterId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string ActionLabel { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
    }

    public class NotificationsResponse
    {
        public List<Notification> Notifications { get; set; } = [];
    }
}
