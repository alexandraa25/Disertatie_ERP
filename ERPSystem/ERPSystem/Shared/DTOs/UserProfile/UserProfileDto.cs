namespace ERPSystem.Shared.DTOs.UserProfile
{
    public record UserProfileDto(
    string FirstName,
    string LastName,
    string? Phone,
    string? JobTitle,
    string? AvatarUrl,
    string PreferredLanguage,
    string TimeZone
);

    public record UpdateUserProfileDto(
        string FirstName,
        string LastName,
        string? Phone,
        string? JobTitle,
        string? AvatarUrl,
        string PreferredLanguage,
        string TimeZone
    );
}
