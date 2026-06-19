using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Data.Entities
{
    public enum NotificationChannel { InApp = 1, Email = 2 }
    public enum DigestMode { Immediate = 1, Daily = 2 }
    public class UserNotificationSetting
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = default!;

        [Required, MaxLength(60)]
        public string EventType { get; set; } = default!; 

        [Required]
        public NotificationChannel Channel { get; set; }

        public bool Enabled { get; set; } = true;

        public DigestMode Digest { get; set; } = DigestMode.Immediate;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
