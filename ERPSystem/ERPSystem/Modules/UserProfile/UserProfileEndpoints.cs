using ERPSystem.Extensions;
using ERPSystem.Models.Notifications;
using ERPSystem.Modules.UserProfile.Models;
using Route = ERPSystem.Utils.Constants.General.Route.Profile;

namespace ERPSystem.Modules.UserProfile;

public static class UserProfileEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
       

        group.MapGet(Route.PROFILE, 
            async (UserProfileService service) 
                => await service.GetProfileAsync())
          .WithDefaultApiSettings("GetProfile", "Get Profile", "GET_PROFILE", true);

      
        group.MapPut(Route.PROFILE, 
            async (UpdateUserProfileDto body,   UserProfileService service)
                => await service.UpdateProfileAsync(body))
        .WithDefaultApiSettings("UpdateProfile", "Update Profile", "UPDATE_PROFILE", true);


        group.MapGet(Route.NOTIFICATION_SETTINGS,
            async (UserProfileService service)
                => await service.GetNotificationSettingsAsync())
        .WithDefaultApiSettings("GetNotificationSettings", "Get Notification Settings", "GET_NOTIFICATION_SETTINGS", true);

       
        group.MapPut(Route.NOTIFICATION_SETTINGS,
            async (List<NotificationSettingDto> body,
                   UserProfileService service)
                => await service.UpsertNotificationSettingsAsync(body))
        .WithDefaultApiSettings("UpdateNotificationSettings", "Update Notification Settings", "UPDATE_NOTIFICATION_SETTINGS", true);
    }
}