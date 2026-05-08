using ERPSystem.Data.Entities;

namespace ERPSystem.Models.Notifications
{
    public class NotificationSettingDto
    {
        public string EventType { get; set; }
        public NotificationChannel Channel { get; set; }
        public bool Enabled { get; set; }
        public DigestMode Digest { get; set; }
    }
}