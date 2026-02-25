using ERPSystem.Shared.DTOs.UserProfile;

namespace ERPSystem.Modules.UserProfile;

public static class UserProfileEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        // GET /me
        group.MapGet("", async (HttpContext ctx, UserProfileBusinessLogic bl) =>
        {
            var me = await bl.GetMeAsync(ctx.User);
            return Results.Ok(me);
        });

        // GET /me/profile
        group.MapGet("/profile", async (HttpContext ctx, UserProfileBusinessLogic bl) =>
        {
            var profile = await bl.GetProfileAsync(ctx.User);
            return Results.Ok(profile);
        });

        // PUT /me/profile
        group.MapPut("/profile", async (HttpContext ctx, UpdateUserProfileDto body, UserProfileBusinessLogic bl) =>
        {
            await bl.UpdateProfileAsync(ctx.User, body);
            return Results.NoContent();
        });

        // GET /me/notification-settings
        group.MapGet("/notification-settings", async (HttpContext ctx, UserProfileBusinessLogic bl) =>
        {
            var items = await bl.GetNotificationSettingsAsync(ctx.User);
            return Results.Ok(items);
        });

        // PUT /me/notification-settings
        group.MapPut("/notification-settings", async (HttpContext ctx, List<NotificationSettingDto> body, UserProfileBusinessLogic bl) =>
        {
            await bl.UpsertNotificationSettingsAsync(ctx.User, body);
            return Results.NoContent();
        });
    }
}
