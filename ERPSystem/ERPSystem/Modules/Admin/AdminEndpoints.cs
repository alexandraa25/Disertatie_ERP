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
              .WithDefaultApiSettings("GetUsers", "Get All Users", "GET_USERS", true);

            group.MapGet(Route.USER_DETAILS,
               async (string userId, AdminService service) =>
                   await service.GetProfileByUserIdAsync(userId))
               .WithDefaultApiSettings( "GetUserDetails",  "Get User Details", "GET_USER_DETAILS",  true);

            group.MapPut(Route.TOGGLE_USER_STATUS,
               async (string userId, AdminService service) =>
                   await service.ToggleUserStatusAsync(userId))
               .WithDefaultApiSettings( "ToggleUserStatus", "Toggle User Status","TOGGLE_USER_STATUS", true);

            group.MapPut(Route.UPDATE_USER_ROLES,
               async (UpdateUserRolesRequest request, AdminService service) =>
                   await service.UpdateUserRolesAsync(request))
               .WithDefaultApiSettings(  "UpdateUserRoles",  "Update User Roles", "UPDATE_USER_ROLES",  true );

            group.MapPut(Route.CONFIRM_USER_EMAIL,
                async (string userId, AdminService service) =>
                    await service.ConfirmUserEmailAsync(userId))
                .WithDefaultApiSettings(  "ConfirmUserEmail", "Confirm User Email", "CONFIRM_USER_EMAIL", true );

            group.MapGet(Route.EMPLOYEES_WITHOUY_USER, 
               async (AdminService service)
                  => await service.GetEmployeesWithoutUserAsync())
               .WithDefaultApiSettings("GetEmployeesWithoutUser", "Get Employees Without User", "GET_EMPLOYEES_WITHOUY_USER", true);

            group.MapGet(Route.USER_ACTIVITY_LOG,
               async (string userId, AdminService service) 
                 => await service.GetUserActivityLogAsync(userId))
              .WithDefaultApiSettings("GetUserActivityLog", "Get User Activity Log", "GET_USER_ACTIVITY_LOG", true); ;
        }
    }
}