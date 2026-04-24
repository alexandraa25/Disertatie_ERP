using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.UserProfile.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;
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


    public async Task<UserProfileDto> GetProfileAsync()
    {
        var userId = GetCurrentUserId();

        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new UnauthorizedAccessException();

        var roles = (await _userManager.GetRolesAsync(user)).ToArray();

        var unreadNotifications = await _applicationDbContext.UserNotificationSettings
            .CountAsync(x => x.UserId == userId && !x.Enabled);

        var employee = await _applicationDbContext.Employees
            .Where(e => e.UserId == userId)
            .Select(e => new
            {
                e.Id,
                e.JobTitle,
                e.HireDate,
                e.Salary,
                e.ContractType,
                e.EmploymentStatus
            })
            .FirstOrDefaultAsync();

        return new UserProfileDto(
            user.FirstName,
            user.LastName,
            user.FullName,
            user.UserName,
            user.Email,
            user.EmailConfirmed,
            user.PhoneNumber,
            roles,
            user.IsActive,
            user.BirthdayDate,
            user.CreatedAt,
            user.LastLoginAt,
            user.AvatarUrl,
            unreadNotifications,
             employee?.Id,
            employee?.JobTitle,
            employee?.HireDate,
            employee?.Salary,
            employee?.ContractType,
            employee?.EmploymentStatus
        );
    }
    public async Task UpdateProfileAsync(UpdateUserProfileDto body)
    {
        var userId = GetCurrentUserId();

        await using var transaction = await _applicationDbContext.Database.BeginTransactionAsync();

        var user = await _applicationDbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null)
            throw new Exception("User not found");

        user.FirstName = body.FirstName?.Trim();
        user.LastName = body.LastName?.Trim();
        user.PhoneNumber = body.PhoneNumber?.Trim();
        user.BirthdayDate = body.BirthdayDate;
        user.AvatarUrl = body.AvatarUrl;

        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = userId;

        await _applicationDbContext.SaveChangesAsync();
        await transaction.CommitAsync();
    }

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