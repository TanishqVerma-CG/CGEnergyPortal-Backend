using System.Xml.Linq;
using BP.IService;
using BP.Models;

namespace BP.Services
{
    public class NotificationService : INotificationService
    {
        private readonly string _xmlFilePath;

        public NotificationService(IWebHostEnvironment env)
        {
            _xmlFilePath = Path.Combine(env.ContentRootPath, "Data", "notifications.xml");
        }

        public NotificationsResponse GetNotifications()
        {
            if (!File.Exists(_xmlFilePath))
                throw new FileNotFoundException($"Notifications data file not found: {_xmlFilePath}");

            var doc = XDocument.Load(_xmlFilePath);
            var notifications = doc.Root!
                .Element("Notifications")!
                .Elements("Notification")
                .Select(e => new Notification
                {
                    NotificationId = (int)e.Element("NotificationId")!,
                    Type           = (string)e.Element("Type")!,
                    MeterId        = (string)e.Element("MeterId")!,
                    Message        = (string)e.Element("Message")!,
                    Time           = (string)e.Element("Time")!,
                    Date           = (string)e.Element("Date")!,
                    ActionLabel    = (string)e.Element("ActionLabel")!,
                    ActionType     = (string)e.Element("ActionType")!
                })
                .ToList();

            return new NotificationsResponse { Notifications = notifications };
        }

        public void AddNotification(Notification notification)
        {
            if (!File.Exists(_xmlFilePath))
                throw new FileNotFoundException($"Notifications data file not found: {_xmlFilePath}");

            var doc = XDocument.Load(_xmlFilePath);
            var notificationsEl = doc.Root!.Element("Notifications")!;

            int nextId = notificationsEl
                .Elements("Notification")
                .Select(e => (int?)e.Element("NotificationId") ?? 0)
                .DefaultIfEmpty(0)
                .Max() + 1;

            notificationsEl.AddFirst(new XElement("Notification",
                new XElement("NotificationId", nextId),
                new XElement("Type",        notification.Type),
                new XElement("MeterId",      notification.MeterId),
                new XElement("Message",      notification.Message),
                new XElement("Time",         notification.Time),
                new XElement("Date",         notification.Date),
                new XElement("ActionLabel",  notification.ActionLabel),
                new XElement("ActionType",   notification.ActionType)
            ));

            doc.Save(_xmlFilePath);
        }
    }
}
