using ERPSystem.Extensions;
using ERPSystem.Modules.UserProfile.Models;
using Route = ERPSystem.Utils.Constants.General.Route.Profile;

namespace ERPSystem.Modules.UserProfile;

public static class UserProfileEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        // ===============================
        // GET ME
        // ===============================
        group.MapGet(Route.ME,  async (UserProfileService service) => await service.GetMeAsync())
            .WithDefaultApiSettings("GetMe", "Get ME", "ME", true);


        // ===============================
        // GET PROFILE
        // ===============================
        group.MapGet(Route.PROFILE, async (UserProfileService service)  => await service.GetProfileAsync())
          .WithDefaultApiSettings("GetProfile", "Get Profile", "GET_PROFILE", true);


        // ===============================
        // UPDATE PROFILE
        // ===============================
      
        group.MapPut(Route.PROFILE, async (UpdateUserProfileDto body,   UserProfileService service)
                => await service.UpdateProfileAsync(body))
        .WithDefaultApiSettings("UpdateProfile", "Update Profile", "UPDATE_PROFILE", true);


        // ===============================
        // GET NOTIFICATION SETTINGS
        // ===============================
        group.MapGet(Route.NOTIFICATION_SETTINGS,
            async (UserProfileService service)
                => await service.GetNotificationSettingsAsync())
        .WithDefaultApiSettings("GetNotificationSettings", "Get Notification Settings", "GET_NOTIFICATION_SETTINGS", true);


        // ===============================
        // UPDATE NOTIFICATION SETTINGS
        // ===============================
        group.MapPut(Route.NOTIFICATION_SETTINGS,
            async (List<NotificationSettingDto> body,
                   UserProfileService service)
                => await service.UpsertNotificationSettingsAsync(body))
        .WithDefaultApiSettings("UpdateNotificationSettings", "Update Notification Settings", "UPDATE_NOTIFICATION_SETTINGS", true);
    }
}