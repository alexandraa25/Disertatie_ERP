using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Modules.UserProfile.Models
{
    public class UserProfile
    {
        [Key]
        public string UserId { get; set; } = default!;  // PK + FK către AspNetUsers

        [MaxLength(80)]
        public string FirstName { get; set; } = "";

        [MaxLength(80)]
        public string LastName { get; set; } = "";

        [MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(120)]
        public string? JobTitle { get; set; }

        [MaxLength(300)]
        public string? AvatarUrl { get; set; }

        [MaxLength(10)]
        public string PreferredLanguage { get; set; } = "ro";

        [MaxLength(64)]
        public string TimeZone { get; set; } = "Europe/Bucharest";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
