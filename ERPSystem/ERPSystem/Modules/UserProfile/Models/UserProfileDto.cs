using ERPSystem.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Modules.UserProfile.Models
{

    public record UserProfileDto(
         string? FirstName,
    string? LastName,
    string? FullName,
    string? Username,
    string? Email,
    bool EmailConfirmed,
    string? PhoneNumber,
    string[] Roles,
    bool IsActive,
    DateTime? BirthdayDate,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    string? AvatarUrl,
    int UnreadNotifications
    );

    public record UpdateUserProfileDto(
      string? FirstName,
      string? LastName,
      string? PhoneNumber,
      DateTime? BirthdayDate,
      string? AvatarUrl
  );



    public record NotificationSettingDto
  (
      [Required] string EventType,
      NotificationChannel Channel,   // 🔥 ENUM — NU string
      bool Enabled,
      DigestMode Digest
  );


}
