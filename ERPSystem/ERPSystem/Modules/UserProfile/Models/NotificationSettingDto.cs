using ERPSystem.Data.Entities;
using static ERPSystem.Utils.Constants.General.Route;

namespace ERPSystem.Models.Notifications
{
    public record NotificationSettingDto(
        string EventType,
        NotificationChannel Channel,
        bool Enabled,
        DigestMode Digest
    );

    
}
