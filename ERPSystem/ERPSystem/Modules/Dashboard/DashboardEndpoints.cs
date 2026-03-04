using ERPSystem.Extensions;
using ERPSystem.Modules.Dashboard;
using Route = ERPSystem.Utils.Constants.General.Route.Dashboard;

public class DashboardEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet(Route.DASHBOARD, async (DashboardService dashboardService)
            => await dashboardService.GetDashboardAsync())
        .WithDefaultApiSettings(
            "GetDashboard",
            "Dashboard statistici",
            "GET",
            true
        );
    }
}