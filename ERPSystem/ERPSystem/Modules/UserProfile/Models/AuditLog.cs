using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Modules.UserProfile.Models
{
    public class AuditLog
    {
        public long Id { get; set; }

        [Required]
        public string UserId { get; set; } = default!;

        [Required, MaxLength(60)]
        public string ActionType { get; set; } = default!; // ex: "ProfileUpdated"

        [MaxLength(80)]
        public string? EntityType { get; set; } // ex: "UserProfile"

        [MaxLength(64)]
        public string? EntityId { get; set; } // ex: userId

        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        public string? MetadataJson { get; set; } // IP, user-agent, etc.
    }
}
