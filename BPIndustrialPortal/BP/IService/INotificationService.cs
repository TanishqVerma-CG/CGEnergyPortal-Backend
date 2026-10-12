using BP.Models;

namespace BP.IService
{
    public interface INotificationService
    {
        NotificationsResponse GetNotifications();
        void AddNotification(Notification notification);
    }
}
