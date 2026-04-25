

using ERPSystem.Extensions;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Helpers;
using Route = ERPSystem.Utils.Constants.General.Route.Notification;
namespace ERPSystem.Shared.ActivityLogs
{
    public class NotificationsEndpoint
    {

        public static void Map(RouteGroupBuilder group)
        {

            group.MapGet(Route.GET_MY_NOTIFICATIONS,
    async (NotificationsService notificationService)
        => await notificationService.GetMyNotifications())
   .WithDefaultApiSettings("GetMyNotifications", "Lista notificari utilizator", "GET", false);


            group.MapPost(Route.MARK_AS_READ,
                async (int id, NotificationsService notificationService)
                    => await notificationService.MarkAsRead(id))
               .WithDefaultApiSettings("MarkAsRead", "Marcheaza notificare ca citita", "POST", false);


            group.MapPost(Route.MARK_ALL_AS_READ,
                async (NotificationsService notificationService)
                    => await notificationService.MarkAllAsRead())
               .WithDefaultApiSettings("MarkAllAsRead", "Marcheaza toate notificarile ca citite", "POST", false);

            group.MapGet(Route.GET_UNREAD_COUNT,
    async (NotificationsService notificationService)
        => await notificationService.GetUnreadCount())
   .WithDefaultApiSettings("GetUnreadCount", "Numar notificari necitite", "GET", false);

            group.MapPost(Route.MARK_ALL_AS_SEEN,
    async (NotificationsService notificationService)
        => await notificationService.MarkAllAsSeen())
   .WithDefaultApiSettings("MarkAllAsSeen", "Marcheaza notificarile ca vazute", "POST", false);
        }
    }
}