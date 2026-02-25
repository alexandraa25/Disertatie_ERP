using System.Security.Claims;
using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.UserProfile.Models;
using ERPSystem.Shared.DTOs.UserProfile;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Modules.UserProfile;

public class UserProfileBusinessLogic
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserProfileBusinessLogic(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<MeDto> GetMeAsync(ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();
        var identityUser = await _userManager.FindByIdAsync(userId) ?? throw new UnauthorizedAccessException();

        var roles = (await _userManager.GetRolesAsync(identityUser)).ToArray();

        var profile = await _db.UserProfiles.FirstOrDefaultAsync(x => x.UserId == userId);
        if (profile is null)
        {
            profile = new Models.UserProfile { UserId = userId };
            _db.UserProfiles.Add(profile);
            await _db.SaveChangesAsync();
        }

        var profileDto = new UserProfileDto(
            profile.FirstName, profile.LastName, profile.Phone, profile.JobTitle,
            profile.AvatarUrl, profile.PreferredLanguage, profile.TimeZone
        );

        return new MeDto(
            UserId: userId,
            Email: identityUser.Email ?? "",
            EmailConfirmed: identityUser.EmailConfirmed,
            Roles: roles,
            Profile: profileDto,
            UnreadNotificationsCount: 0
        );
    }

    public async Task<UserProfileDto> GetProfileAsync(ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();
        var profile = await _db.UserProfiles.FirstOrDefaultAsync(x => x.UserId == userId);

        if (profile is null)
            return new UserProfileDto("", "", null, null, null, "ro", "Europe/Bucharest");

        return new UserProfileDto(
            profile.FirstName, profile.LastName, profile.Phone, profile.JobTitle,
            profile.AvatarUrl, profile.PreferredLanguage, profile.TimeZone
        );
    }

    public async Task UpdateProfileAsync(ClaimsPrincipal user, UpdateUserProfileDto body)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();
        var profile = await _db.UserProfiles.FirstOrDefaultAsync(x => x.UserId == userId);

        if (profile is null)
        {
            profile = new Models.UserProfile { UserId = userId };
            _db.UserProfiles.Add(profile);
        }

        profile.FirstName = body.FirstName.Trim();
        profile.LastName = body.LastName.Trim();
        profile.Phone = string.IsNullOrWhiteSpace(body.Phone) ? null : body.Phone.Trim();
        profile.JobTitle = string.IsNullOrWhiteSpace(body.JobTitle) ? null : body.JobTitle.Trim();
        profile.AvatarUrl = string.IsNullOrWhiteSpace(body.AvatarUrl) ? null : body.AvatarUrl.Trim();
        profile.PreferredLanguage = string.IsNullOrWhiteSpace(body.PreferredLanguage) ? "ro" : body.PreferredLanguage.Trim();
        profile.TimeZone = string.IsNullOrWhiteSpace(body.TimeZone) ? "Europe/Bucharest" : body.TimeZone.Trim();
        profile.UpdatedAt = DateTime.UtcNow;

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            ActionType = "ProfileUpdated",
            EntityType = "UserProfile",
            EntityId = userId,
            TimestampUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }

    public async Task<List<NotificationSettingDto>> GetNotificationSettingsAsync(ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();

        return await _db.UserNotificationSettings
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.EventType).ThenBy(x => x.Channel)
            .Select(x => new NotificationSettingDto(x.EventType, x.Channel, x.Enabled, x.Digest))
            .ToListAsync();
    }

    public async Task UpsertNotificationSettingsAsync(ClaimsPrincipal user, List<NotificationSettingDto> body)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();

        foreach (var dto in body)
        {
            var existing = await _db.UserNotificationSettings.FirstOrDefaultAsync(x =>
                x.UserId == userId && x.EventType == dto.EventType && x.Channel == dto.Channel);

            if (existing is null)
            {
                _db.UserNotificationSettings.Add(new UserNotificationSetting
                {
                    UserId = userId,
                    EventType = dto.EventType,
                    Channel = dto.Channel,
                    Enabled = dto.Enabled,
                    Digest = dto.Digest,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.Enabled = dto.Enabled;
                existing.Digest = dto.Digest;
                existing.UpdatedAt = DateTime.UtcNow;
            }
        }

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            ActionType = "NotificationSettingsUpdated",
            EntityType = "UserNotificationSetting",
            EntityId = userId,
            TimestampUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }
}
