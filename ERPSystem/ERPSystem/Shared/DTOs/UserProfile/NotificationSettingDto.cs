using ERPSystem.Modules.UserProfile.Models;

namespace ERPSystem.Shared.DTOs.UserProfile;

public record NotificationSettingDto(
    string EventType,
    NotificationChannel Channel,
    bool Enabled,
    DigestMode Digest
);