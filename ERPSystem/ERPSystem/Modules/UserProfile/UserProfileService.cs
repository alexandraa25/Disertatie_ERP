using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Models.Notifications;
using ERPSystem.Modules.UserProfile.Models;
using ERPSystem.Shared.Notifications;
using ERPSystem.Utils.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;
namespace ERPSystem.Modules.UserProfile;



public class UserProfileService
{
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly NotificationsService _notificationService;

    public UserProfileService(
        ApplicationDbContext applicationDbContex,
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor httpContextAccessor, NotificationsService notificationService)
    {
        _applicationDbContext = applicationDbContex;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
        _notificationService = notificationService;
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

    public async Task<PublicResponse> GetProfileAsync()
    {
        var response = new PublicResponse(true);

        try
        {
            var userId = GetCurrentUserId();

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return response.SetError("NOT_FOUND", "User not found");

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
         e.TerminationDate,
         e.Salary,
         e.ContractType,
         e.EmploymentStatus,

         Address = e.Address == null ? null : new AddressDto
         {
             Street = e.Address.Street,
             City = e.Address.City,
             Country = e.Address.Country,
             PostalCode = e.Address.PostalCode
         },

         Contact = e.Contact == null ? null : new ContactDto
         {
             PhoneNumber = e.Contact.PhoneNumber,
             EmergencyContactName = e.Contact.EmergencyContactName,
             EmergencyContactPhone = e.Contact.EmergencyContactPhone
         },

         Bank = e.Bank == null ? null : new BankDto
         {
             IBAN = e.Bank.IBAN,
             BankName = e.Bank.BankName
         },

         Documents = e.Documents.Select(d => new DocumentDto
         {
             Id = d.Id,
             FileName = d.FileName,
             FilePath = d.FilePath,
             DocumentType = d.DocumentType,
             UploadedAt = d.UploadedAt
         }).ToList()
     })
     .FirstOrDefaultAsync();

            var result = new UserProfileDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                Username = user.UserName,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumber = user.PhoneNumber,
                Roles = roles,
                IsActive = user.IsActive,
                BirthdayDate = user.BirthdayDate,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                AvatarUrl = user.AvatarUrl,
                UnreadNotifications = unreadNotifications,

                EmployeeId = employee?.Id,
                JobTitle = employee?.JobTitle,
                HireDate = employee?.HireDate,
                Salary = employee?.Salary,
                ContractType = employee?.ContractType,
                EmploymentStatus = employee?.EmploymentStatus,
                TerminationDate = employee?.TerminationDate,
                Address = employee?.Address,
                Contact = employee?.Contact,
                Bank = employee?.Bank,
                Documents = employee?.Documents
            };

            return response.SetSuccess(result);
        }
        catch (Exception ex)
        {
            return response.SetError("SERVER", ex.Message);
        }
    }
    public async Task<PublicResponse> UpdateProfileAsync(UpdateUserProfileDto body)
    {
        var response = new PublicResponse(true);

        await using var transaction = await _applicationDbContext.Database.BeginTransactionAsync();

        try
        {
            var userId = GetCurrentUserId();

            var user = await _applicationDbContext.Users
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null)
                return response.SetError("NOT_FOUND", "User not found");

            user.FirstName = body.FirstName?.Trim();
            user.LastName = body.LastName?.Trim();
            user.PhoneNumber = body.PhoneNumber?.Trim();
            user.BirthdayDate = body.BirthdayDate;
            user.AvatarUrl = body.AvatarUrl;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = userId;

            var employee = await _applicationDbContext.Employees
               .FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee != null)
            {
                employee.FirstName = user.FirstName;
                employee.LastName = user.LastName;
                employee.UpdatedAt = DateTime.UtcNow;

                var address = await _applicationDbContext.EmployeeAddress
                    .FirstOrDefaultAsync(x => x.EmployeeId == employee.Id);

                if (address == null)
                {
                    address = new EmployeeAddress
                    {
                        Id = Guid.NewGuid(),
                        EmployeeId = employee.Id,
                        Street = body.Street?.Trim() ?? "",
                        City = body.City?.Trim() ?? "",
                        Country = body.Country?.Trim() ?? "",
                        PostalCode = body.PostalCode?.Trim() ?? ""
                    };

                    _applicationDbContext.EmployeeAddress.Add(address);
                }
                else
                {
                    address.Street = body.Street?.Trim() ?? address.Street;
                    address.City = body.City?.Trim() ?? address.City;
                    address.Country = body.Country?.Trim() ?? address.Country;
                    address.PostalCode = body.PostalCode?.Trim() ?? address.PostalCode;
                }

                var contact = await _applicationDbContext.EmployeeContact
                    .FirstOrDefaultAsync(x => x.EmployeeId == employee.Id);

                if (contact == null)
                {
                    contact = new EmployeeContact
                    {
                        Id = Guid.NewGuid(),
                        EmployeeId = employee.Id,
                        PhoneNumber = body.PhoneNumber?.Trim(),
                        EmergencyContactName = body.EmergencyContactName?.Trim(),
                        EmergencyContactPhone = body.EmergencyContactPhone?.Trim()
                    };

                    _applicationDbContext.EmployeeContact.Add(contact);
                }
                else
                {
                    contact.PhoneNumber = body.PhoneNumber?.Trim() ?? contact.PhoneNumber;
                    contact.EmergencyContactName = body.EmergencyContactName?.Trim() ?? contact.EmergencyContactName;
                    contact.EmergencyContactPhone = body.EmergencyContactPhone?.Trim() ?? contact.EmergencyContactPhone;
                }
            }
            await _applicationDbContext.SaveChangesAsync();


            if (employee != null)
            {

                _applicationDbContext.ActivityLog.Add(new ActivityLog
                {
                    EntityType = "Employee",
                    EntityId = employee.Id.ToString(),
                    Action = "UserProfileUpdated",
                    Description = $"{employee.FirstName} {employee.LastName} și-a actualizat profilul.",
                    CreatedAtUtc = DateTime.UtcNow,
                    PerformedBy = userId
                });

                await _notificationService.CreateNotificationForRolesAsync(
                    roleNames: new[] { "HR", "Administrator" },
                    eventType: NotificationEvents.UserActivity,
                    title: "Profil utilizator actualizat",
                    message: $"{employee.FirstName} {employee.LastName} și-a actualizat profilul.",
                    type: "Info",
                    link: $"/employee/{employee.Id}",
                    entityType: "Employee",
                    entityId: employee.Id.ToString()
                );
            }

            await transaction.CommitAsync();

            return response.SetSuccess(true);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return response.SetError("SERVER", ex.Message);
        }
    }

    public async Task<List<NotificationSettingDto>> GetNotificationSettingsAsync()
    {
        var userId = GetCurrentUserId();

        var existing = await _applicationDbContext.UserNotificationSettings
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToListAsync();

        var result = new List<NotificationSettingDto>();

        foreach (var eventType in NotificationEvents.All)
        {
            foreach (NotificationChannel channel in Enum.GetValues<NotificationChannel>())
            {
                var setting = existing.FirstOrDefault(x =>
                    x.EventType == eventType &&
                    x.Channel == channel);

                result.Add(new NotificationSettingDto(
                    eventType,
                    channel,
                    setting?.Enabled ?? true,
                    setting?.Digest ?? GetDefaultDigest(channel)
                ));
            }
        }

        return result
            .OrderBy(x => x.EventType)
            .ThenBy(x => x.Channel)
            .ToList();
    }

    public async Task UpsertNotificationSettingsAsync(List<NotificationSettingDto> body)
    {
        var userId = GetCurrentUserId();

        var existingSettings = await _applicationDbContext.UserNotificationSettings
            .Where(x => x.UserId == userId)
            .ToListAsync();

        foreach (var dto in body)
        {
            if (!NotificationEvents.All.Contains(dto.EventType))
                continue;

            var existing = existingSettings.FirstOrDefault(x =>
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

    private static DigestMode GetDefaultDigest(NotificationChannel channel)
    {
        return channel == NotificationChannel.Email
            ? DigestMode.Daily
            : DigestMode.Immediate;
    }
}