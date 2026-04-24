using ERPSystem.Extensions;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Helpers;
using Route = ERPSystem.Utils.Constants.General.Route.General;
namespace ERPSystem.Shared.ActivityLogs
{
    public class ActivityLogEndpoint
    {

        public static void Map(RouteGroupBuilder group)
        {
            group.MapGet(Route.ACTIVITY,
                async (string entity, string id, ActivityLogService activityLogService)
                    => await activityLogService.GetActivity(entity, id))
               .WithDefaultApiSettings("GetActivity", "Istoric generic entitate", "GET", false);

            group.MapGet(Route.ACTIVITY_ALL,
                async ([FromQuery] string[]? entity, [FromQuery] string[]? action, [FromQuery] string[]? performedBy, DateTime ? from, DateTime? to, int page, int pageSize, ActivityLogService activityLogService)
                    => await activityLogService.GetAllActivity(entity?.ToList(), action?.ToList(), performedBy?.ToList(), from, to, page, pageSize))
               .WithDefaultApiSettings("GetAllActivity", "Istoric activitati", "GET", false);

            group.MapGet(Route.FILLTER_OPTIONS,
                async ( ActivityLogService activityLogService)
                    => await activityLogService.GetFilterOptions())
               .WithDefaultApiSettings("GetFilterOptions", "Optiuni pentru filre", "GET", false);



        }
    }
}
