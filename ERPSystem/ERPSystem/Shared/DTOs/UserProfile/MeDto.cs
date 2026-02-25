namespace ERPSystem.Shared.DTOs.UserProfile
{
    public record MeDto(
    string UserId,
    string Email,
    bool EmailConfirmed,
    string[] Roles,
    UserProfileDto Profile,
    int UnreadNotificationsCount
);
}
