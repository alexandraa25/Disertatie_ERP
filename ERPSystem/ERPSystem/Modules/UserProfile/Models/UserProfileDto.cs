using ERPSystem.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Modules.UserProfile.Models
{

    public record UserProfileDto(
        string? FirstName,
        string? LastName,
        string? Phone,
        string? JobTitle,
        string? AvatarUrl,
        string PreferredLanguage,
        string TimeZone
    );

    public record UpdateUserProfileDto(
    [property: MaxLength(80)] string FirstName,
    [property: MaxLength(80)] string LastName,
    [property: MaxLength(30)] string? Phone,
    [property: MaxLength(300)] string? AvatarUrl,
    string? PreferredLanguage,
    string? TimeZone
);

    public record MeDto(
   string UserId,
   string Email,
   bool EmailConfirmed,
   string[] Roles,
   UserProfileDto Profile,
   int UnreadNotificationsCount
);

    public record NotificationSettingDto
  (
      [Required] string EventType,
      NotificationChannel Channel,   // 🔥 ENUM — NU string
      bool Enabled,
      DigestMode Digest
  );


}
