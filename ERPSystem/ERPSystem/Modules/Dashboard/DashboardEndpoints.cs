using ERPSystem.Extensions;
using ERPSystem.Modules.Dashboard;
using Route = ERPSystem.Utils.Constants.General.Route.Dashboard;

public class DashboardEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet(Route.DASHBOARD_OVERVIEW,
           async (DashboardService dashboardService) =>
               await dashboardService.GetOverviewAsync())
           .WithDefaultApiSettings( "GetOverviewDashboard",  "Gets overview dashboard statistics", "GET_OVERVIEW_DASHBOARD", false );

        group.MapGet(Route.DASHBOARD_FINANCIAL,
           async (DashboardService dashboardService) =>
               await dashboardService.GetFinancialAsync())
           .WithDefaultApiSettings( "GetFinancialDashboard", "Gets financial dashboard statistics", "GET_FINANCIAL_DASHBOARD", false );

        group.MapGet(Route.DASHBOARD_EDUCATION,
           async (DashboardService dashboardService) =>
               await dashboardService.GetEducationAsync())
           .WithDefaultApiSettings( "GetEducationDashboard", "Gets education dashboard statistics", "GET_EDUCATION_DASHBOARD", false );

        group.MapGet(Route.DASHBOARD_HR,
           async (DashboardService dashboardService) =>
               await dashboardService.GetHrAsync())
           .WithDefaultApiSettings( "GetHrDashboard", "Gets HR dashboard statistics", "GET_HR_DASHBOARD", false );
    }
}