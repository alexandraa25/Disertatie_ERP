namespace ERPSystem.Modules.Admin.Models
{
    public class UpdateUserRolesRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string[] Roles { get; set; } = Array.Empty<string>();
    }
}
