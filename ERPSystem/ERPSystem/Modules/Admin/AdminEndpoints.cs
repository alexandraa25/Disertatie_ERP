using ERPSystem.Extensions;
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

            group.MapGet(Route.EMPLOYEES_WITHOUY_USER, 
               async (AdminService service)
                  => await service.GetEmployeesWithoutUserAsync())
               .WithDefaultApiSettings("GetEmployeesWithoutUser", "Get Employees Without User", "GET_EMPLOYEES_WITHOUY_USER", true);
        }
    }
}