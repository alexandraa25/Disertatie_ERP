using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.Admin.Models;
using ERPSystem.Modules.UserProfile.Models;
using ERPSystem.Shared.Notifications;
using ERPSystem.Utils.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Modules.Admin
{
    public class AdminService
    {
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly NotificationsService _notificationService;


        public AdminService(
            ApplicationDbContext applicationDbContex,
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor httpContextAccessor,
            NotificationsService notificationService)
        {
            _applicationDbContext = applicationDbContex;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
        }

        public async Task<IResult> GetDashboardAsync()
        {
            var users = await _userManager.Users
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var userDtos = new List<CompanyUserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                userDtos.Add(new CompanyUserDto
                {
                    Id = user.Id,
                    Username = user.UserName,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    Roles = roles.ToList()
                });
            }

            var dashboard = new AdminDashboardDto
            {
                TotalUsers = userDtos.Count,
                ActiveUsers = userDtos.Count(x => x.IsActive),
                InactiveUsers = userDtos.Count(x => !x.IsActive),
                AdminUsers = userDtos.Count(x => x.Roles.Contains("Admin")),
                LatestUsers = userDtos.Take(3).ToList(),
                Users = userDtos
            };

            return Results.Ok(dashboard);
        }

        public async Task<IResult> GetEmployeesWithoutUserAsync()
        {
            var employees = await _applicationDbContext.Employees
                  .Where(e => e.UserId == null)
                  .Select(e => new
                  {
                      e.Id,
                      e.FirstName,
                      e.LastName,
                      e.Email,
                      e.JobTitle,
                      PhoneNumber = e.Contact != null ? e.Contact.PhoneNumber : null
                  })
                  .ToListAsync();

            return Results.Ok(employees);
        }

        public async Task<PublicResponse> GetProfileByUserIdAsync(string userId)
        {
            var response = new PublicResponse(true);

            try
            {
                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                    return response.SetError("NOT_FOUND", "Utilizatorul nu a fost găsit.");

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
                        e.EmploymentStatus,

                        Address = e.Address == null ? null : new AddressDto(
                            e.Address.Street,
                            e.Address.City,
                            e.Address.Country,
                            e.Address.PostalCode
                        ),

                        Contact = e.Contact == null ? null : new ContactDto(
                            e.Contact.PhoneNumber,
                            e.Contact.EmergencyContactName,
                            e.Contact.EmergencyContactPhone
                        ),

                        Bank = e.Bank == null ? null : new BankDto(
                            e.Bank.IBAN,
                            e.Bank.BankName
                        ),

                        Documents = e.Documents.Select(d => new DocumentDto(
                            d.Id,
                            d.FileName,
                            d.FilePath,
                            d.DocumentType,
                            d.UploadedAt
                        )).ToList()
                    })
                    .FirstOrDefaultAsync();

                var result = new UserProfileDto(
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
                    employee?.EmploymentStatus,

                    employee?.Address,
                    employee?.Contact,
                    employee?.Bank,
                    employee?.Documents
                );

                return response.SetSuccess(result);
            }
            catch (Exception ex)
            {
                return response.SetError("SERVER", ex.Message);
            }
        }

        public async Task<PublicResponse> ToggleUserStatusAsync(string userId)
        {
            var response = new PublicResponse(true);

            try
            {
                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                    return response.SetError("NOT_FOUND", "Utilizatorul nu a fost găsit.");

                user.IsActive = !user.IsActive;

                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                    return response.SetError("UPDATE_FAILED", "Statusul utilizatorului nu a putut fi actualizat.");

                await AddActivityLogAsync(
                    user.Id,
                    user.IsActive ? "UserActivated" : "UserDeactivated",
                    user.IsActive
                        ? $"Contul utilizatorului {user.Email} a fost activat."
                        : $"Contul utilizatorului {user.Email} a fost dezactivat."
                );

                await _notificationService.CreateNotificationAsync(
                    userId: user.Id,
                    eventType:  NotificationEvents.UserActivity,
                    title: user.IsActive ? "Cont activat" : "Cont dezactivat",
                    message: user.IsActive
                        ? "Contul tău a fost activat."
                        : "Contul tău a fost dezactivat.",
                    type: user.IsActive ? "Success" : "Warning",
                    link: "/profil-user",
                    entityType: "User",
                    entityId: user.Id
                );

                return response.SetSuccess(new
                {
                    user.Id,
                    user.IsActive
                });
            }
            catch (Exception ex)
            {
                return response.SetError("SERVER", ex.Message);
            }
        }

        public async Task<PublicResponse> UpdateUserRolesAsync(UpdateUserRolesRequest request)
        {
            var response = new PublicResponse(true);

            try
            {
                var user = await _userManager.FindByIdAsync(request.UserId);

                if (user == null)
                    return response.SetError("NOT_FOUND", "Utilizatorul nu a fost găsit.");

                var currentRoles = await _userManager.GetRolesAsync(user);

                var oldRoles = currentRoles.ToArray();

                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);

                if (!removeResult.Succeeded)
                    return response.SetError("REMOVE_FAILED", "Rolurile existente nu au putut fi eliminate.");

                var addResult = await _userManager.AddToRolesAsync(user, request.Roles);

                if (!addResult.Succeeded)
                    return response.SetError("ADD_FAILED", "Rolurile noi nu au putut fi adăugate.");
                await AddActivityLogAsync(
                     user.Id,
                     "UserRolesUpdated",
                     $"Rolurile utilizatorului {user.Email} au fost modificate din [{string.Join(", ", oldRoles)}] în [{string.Join(", ", request.Roles)}]."
                 );

                await _notificationService.CreateNotificationAsync(
                    userId: user.Id,
                    eventType: NotificationEvents.UserActivity,
                    title: "Roluri actualizate",
                    message: $"Rolurile contului tău au fost actualizate: {string.Join(", ", request.Roles)}.",
                    type: "Info",
                    link: "/profil-user",
                    entityType: "User",
                    entityId: user.Id
                );

                return response.SetSuccess(new
                {
                    user.Id,
                    Roles = request.Roles
                });
            }
            catch (Exception ex)
            {
                return response.SetError("SERVER", ex.Message);
            }
        }

        public async Task<PublicResponse> ConfirmUserEmailAsync(string userId)
        {
            var response = new PublicResponse(true);

            try
            {
                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                    return response.SetError("NOT_FOUND", "Utilizatorul nu a fost găsit.");

                user.EmailConfirmed = true;

                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                    return response.SetError("UPDATE_FAILED", "Emailul nu a putut fi confirmat.");

                await AddActivityLogAsync(
                    user.Id,
                    "UserEmailConfirmed",
                    $"Emailul utilizatorului {user.Email} a fost confirmat manual."
                );

                await _notificationService.CreateNotificationAsync(
                     userId: user.Id,
                     eventType: NotificationEvents.UserActivity,
                     title: "Email confirmat",
                     message: "Emailul contului tău a fost confirmat.",
                     type: "Success",
                     link: "/profil-user",
                     entityType: "User",
                     entityId: user.Id
                 );

                return response.SetSuccess(new
                {
                    user.Id,
                    user.EmailConfirmed
                });
            }
            catch (Exception ex)
            {
                return response.SetError("SERVER", ex.Message);
            }
        }

        public async Task<PublicResponse> GetUserActivityLogAsync(string userId)
        {
            var response = new PublicResponse(true);

            try
            {
                var logs = await _applicationDbContext.ActivityLog
                    .Where(x => x.EntityType == "User" && x.EntityId == userId)
                    .OrderByDescending(x => x.CreatedAtUtc)
                    .Select(x => new
                    {
                        x.Id,
                        x.Action,
                        x.Description,
                        x.CreatedAtUtc,
                        x.PerformedBy
                    })
                    .ToListAsync();

                return response.SetSuccess(logs);
            }
            catch (Exception ex)
            {
                return response.SetError("SERVER", ex.Message);
            }
        }


        private string GetCurrentAdminEmail()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.Name
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst("email")?.Value
                ?? "system";
        }

        private async Task AddActivityLogAsync( string entityId,string action, string description)
        {
            _applicationDbContext.ActivityLog.Add(new ActivityLog
            {
                EntityType = "User",
                EntityId = entityId,
                Action = action,
                Description = description,
                CreatedAtUtc = DateTime.UtcNow,
                PerformedBy = GetCurrentAdminEmail()
            });

            await _applicationDbContext.SaveChangesAsync();
        }
    }


}
