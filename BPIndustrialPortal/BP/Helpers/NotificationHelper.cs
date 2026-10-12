using BP.IService;
using BP.Models;

namespace BP.Helpers
{
    public class NotificationHelper(INotificationService notificationService)
    {
        private readonly INotificationService _notificationService = notificationService;

        public void SendSupportRequestNotification(string caseId, string meterId)
        {
            var now = DateTime.Now;
            _notificationService.AddNotification(new Notification
            {
                Type        = "Support Request",
                MeterId     = meterId,
                Message     = $"Support request {caseId} has been received and is being processed.",
                Time        = now.ToString("hh:mm tt"),
                Date        = now.ToString("MM/dd/yyyy"),
                ActionLabel = "View Support Request",
                ActionType  = "support"
            });
        }
    }
}
