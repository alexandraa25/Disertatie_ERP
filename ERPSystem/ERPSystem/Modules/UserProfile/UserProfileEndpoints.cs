using ERPSystem.Modules.UserProfile.Models;
using ERPSystem.Data.Entities;
using Route = ERPSystem.Utils.Constants.General.Route.Profile;


namespace ERPSystem.Modules.UserProfile;

public static class UserProfileEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet(Route.ME,
            async (UserProfileService service, HttpContext ctx)
                => await service.GetMeAsync(ctx.User))
        .WithName("GetMe");

        group.MapGet(Route.PROFILE,
            async (UserProfileService service, HttpContext ctx)
                => await service.GetProfileAsync(ctx.User))
        .WithName("GetProfile");

        group.MapPut(Route.PROFILE,
            async (UpdateUserProfileDto body,
                   UserProfileService service,
                   HttpContext ctx)
                => await service.UpdateProfileAsync(ctx.User, body))
        .WithName("UpdateProfile");

        group.MapGet(Route.NOTIFICATION_SETTINGS,
            async (UserProfileService service, HttpContext ctx)
                => await service.GetNotificationSettingsAsync(ctx.User))
        .WithName("GetNotificationSettings");

        group.MapPut(Route.NOTIFICATION_SETTINGS,
            async (List<NotificationSettingDto> body,
                   UserProfileService service,
                   HttpContext ctx)
                => await service.UpsertNotificationSettingsAsync(ctx.User, body))
        .WithName("UpdateNotificationSettings");
    }
}
