using DocumentFormat.OpenXml.Spreadsheet;
using ERPSystem.Extensions;
using ERPSystem.Modules.Admin.Models;
using ERPSystem.Modules.UserProfile;
using Route = ERPSystem.Utils.Constants.General.Route.Admin;

namespace ERPSystem.Modules.Admin
{
    public class AdminEndpoints
    {
        public static void Map(RouteGroupBuilder group)
        {

            group.MapGet(Route.USERS,
              async (AdminService service)
                  => await service.GetDashboardAsync())
              .RequireAuthorization(policy => policy.RequireRole("Admin", "Manager"))
              .WithDefaultApiSettings("GetUsers", "Returnează lista utilizatorilor", "GET_USERS", true);

            group.MapGet(Route.USER_DETAILS,
              async (string userId, AdminService service) =>
                  await service.GetProfileByUserIdAsync(userId))
              .RequireAuthorization(policy => policy.RequireRole("Admin", "Manager"))
              .WithDefaultApiSettings("GetUserDetails", "Returnează detaliile utilizatorului", "GET_USER_DETAILS", true);

            group.MapPut(Route.TOGGLE_USER_STATUS,
              async (string userId, AdminService service) =>
                  await service.ToggleUserStatusAsync(userId))
              .RequireAuthorization(policy => policy.RequireRole("Admin", "Manager"))
              .WithDefaultApiSettings("ToggleUserStatus", "Activează sau dezactivează un utilizator", "TOGGLE_USER_STATUS", true);

            group.MapPut(Route.UPDATE_USER_ROLES,
              async (UpdateUserRolesRequest request, AdminService service) =>
                  await service.UpdateUserRolesAsync(request))
              .RequireAuthorization(policy => policy.RequireRole("Admin"))
              .WithDefaultApiSettings("UpdateUserRoles", "Actualizează rolurile utilizatorului", "UPDATE_USER_ROLES", true);

            group.MapPut(Route.CONFIRM_USER_EMAIL,
              async (string userId, AdminService service) =>
                  await service.ConfirmUserEmailAsync(userId))
              .RequireAuthorization(policy => policy.RequireRole("Admin", "Manager"))
              .WithDefaultApiSettings("ConfirmUserEmail", "Confirmă manual emailul utilizatorului", "CONFIRM_USER_EMAIL", true);

            group.MapGet(Route.EMPLOYEES_WITHOUY_USER,
              async (AdminService service)
                 => await service.GetEmployeesWithoutUserAsync())
              .RequireAuthorization(policy => policy.RequireRole("Admin", "Manager", "HR"))
              .WithDefaultApiSettings("GetEmployeesWithoutUser", "Returnează angajații fără cont de utilizator", "GET_EMPLOYEES_WITHOUY_USER", true);

            group.MapGet(Route.USER_ACTIVITY_LOG,
              async (string userId, AdminService service)
                 => await service.GetUserActivityLogAsync(userId))
              .RequireAuthorization(policy => policy.RequireRole("Admin", "Manager"))
              .WithDefaultApiSettings("GetUserActivityLog", "Returnează istoricul utilizatorului", "GET_USER_ACTIVITY_LOG", true);
        }
    }
}