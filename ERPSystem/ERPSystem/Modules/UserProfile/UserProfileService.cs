using System.Security.Claims;
using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.UserProfile.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserProfileEntity = ERPSystem.Data.Entities.UserProfile;

namespace ERPSystem.Modules.UserProfile;

public class UserProfileService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserProfileService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // =====================================================
    // GET ME
    // =====================================================
    public async Task<MeDto> GetMeAsync(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? throw new UnauthorizedAccessException();

        var user = await _userManager.FindByIdAsync(userId)
                   ?? throw new UnauthorizedAccessException();

        var roles = (await _userManager.GetRolesAsync(user)).ToArray();

        var profile = await _db.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        profile ??= new UserProfileEntity
        {
            UserId = userId
        };

        return new MeDto(
            userId,
            user.Email ?? string.Empty,
            user.EmailConfirmed,
            roles,
            new UserProfileDto(
                profile.FirstName,
                profile.LastName,
                profile.Phone,
                profile.JobTitle,
                profile.AvatarUrl,
                profile.PreferredLanguage ?? "ro",
                profile.TimeZone ?? "Europe/Bucharest"
            ),
            0
        );
    }

    // =====================================================
    // GET PROFILE
    // =====================================================
    public async Task<UserProfileDto> GetProfileAsync(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? throw new UnauthorizedAccessException();

        var profile = await _db.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (profile == null)
        {
            return new UserProfileDto(
                null,
                null,
                null,
                null,
                null,
                "ro",
                "Europe/Bucharest"
            );
        }

        return new UserProfileDto(
            profile.FirstName,
            profile.LastName,
            profile.Phone,
            profile.JobTitle,
            profile.AvatarUrl,
            profile.PreferredLanguage ?? "ro",
            profile.TimeZone ?? "Europe/Bucharest"
        );
    }

    // =====================================================
    // UPDATE PROFILE
    // =====================================================
    public async Task UpdateProfileAsync(ClaimsPrincipal principal, UpdateUserProfileDto body)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? throw new UnauthorizedAccessException();

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var profile = await _db.UserProfiles
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (profile == null)
        {
            profile = new UserProfileEntity
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _db.UserProfiles.Add(profile);
        }

        profile.FirstName = body.FirstName?.Trim();
        profile.LastName = body.LastName?.Trim();
        profile.Phone = body.Phone?.Trim();
        profile.AvatarUrl = body.AvatarUrl?.Trim();
        profile.PreferredLanguage = body.PreferredLanguage ?? "ro";
        profile.TimeZone = body.TimeZone ?? "Europe/Bucharest";
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    // =====================================================
    // GET NOTIFICATION SETTINGS
    // =====================================================
    public async Task<List<NotificationSettingDto>> GetNotificationSettingsAsync(ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? throw new UnauthorizedAccessException();

        return await _db.UserNotificationSettings
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.EventType)
            .ThenBy(x => x.Channel)
            .Select(x => new NotificationSettingDto(
                x.EventType,
                x.Channel,
                x.Enabled,
                x.Digest
            ))
            .ToListAsync();
    }

    // =====================================================
    // UPSERT NOTIFICATION SETTINGS (OPTIMIZED)
    // =====================================================
    public async Task UpsertNotificationSettingsAsync(
        ClaimsPrincipal user,
        List<NotificationSettingDto> body)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? throw new UnauthorizedAccessException();

        var existingSettings = await _db.UserNotificationSettings
            .Where(x => x.UserId == userId)
            .ToListAsync();

        foreach (var dto in body)
        {
            var existing = existingSettings
                .FirstOrDefault(x =>
                    x.EventType == dto.EventType &&
                    x.Channel == dto.Channel);

            if (existing == null)
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