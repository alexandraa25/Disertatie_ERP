using System.Security.Claims;
using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.UserProfile.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserProfileEntity = ERPSystem.Data.Entities.UserProfile;

namespace ERPSystem.Modules.UserProfile;


//DE FOLOSIT PUBLIC RESPONSE PULIFRICIII!!
public class UserProfileService
{
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserProfileService(
        ApplicationDbContext applicationDbContex,
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _applicationDbContext = applicationDbContex;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }

    // =====================================================
    // GET CURRENT USER FROM TOKEN
    // =====================================================
    private ClaimsPrincipal GetCurrentUser()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user == null || !user.Identity?.IsAuthenticated == true)
            throw new UnauthorizedAccessException();

        return user;
    }

    private string GetCurrentUserId()
    {
        var user = GetCurrentUser();

        return user.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? user.FindFirstValue("sub")
               ?? throw new UnauthorizedAccessException();
    }

    // =====================================================
    // GET ME
    // =====================================================
    public async Task<MeDto> GetMeAsync()
    {
        var userId = GetCurrentUserId();

        var user = await _userManager.FindByIdAsync(userId)
                   ?? throw new UnauthorizedAccessException();

        var roles = (await _userManager.GetRolesAsync(user)).ToArray();

        var profile = await _applicationDbContext.UserProfiles
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
    public async Task<UserProfileDto> GetProfileAsync()
    {
        var userId = GetCurrentUserId();

        var profile = await _applicationDbContext.UserProfiles
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
    public async Task UpdateProfileAsync(UpdateUserProfileDto body)
    {
        var userId = GetCurrentUserId();

        await using var transaction = await _applicationDbContext.Database.BeginTransactionAsync();

        var profile = await _applicationDbContext.UserProfiles
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (profile == null)
        {
            profile = new UserProfileEntity
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _applicationDbContext.UserProfiles.Add(profile);
        }

        profile.FirstName = body.FirstName?.Trim();
        profile.LastName = body.LastName?.Trim();
        profile.Phone = body.Phone?.Trim();
        profile.AvatarUrl = body.AvatarUrl?.Trim();
        profile.PreferredLanguage = body.PreferredLanguage ?? "ro";
        profile.TimeZone = body.TimeZone ?? "Europe/Bucharest";
        profile.UpdatedAt = DateTime.UtcNow;

        await _applicationDbContext.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    // =====================================================
    // GET NOTIFICATION SETTINGS
    // =====================================================
    public async Task<List<NotificationSettingDto>> GetNotificationSettingsAsync()
    {
        var userId = GetCurrentUserId();

        return await _applicationDbContext.UserNotificationSettings
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
    // UPSERT NOTIFICATION SETTINGS
    // =====================================================
    public async Task UpsertNotificationSettingsAsync(List<NotificationSettingDto> body)
    {
        var userId = GetCurrentUserId();

        var existingSettings = await _applicationDbContext.UserNotificationSettings
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
                _applicationDbContext.UserNotificationSettings.Add(new UserNotificationSetting
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

        await _applicationDbContext.SaveChangesAsync();
    }
}