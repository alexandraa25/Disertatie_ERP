namespace ERPSystem.Modules.Authentification.Models
{
    public class ResetPasswordRequest
    {
        public string UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; }
    }
}
