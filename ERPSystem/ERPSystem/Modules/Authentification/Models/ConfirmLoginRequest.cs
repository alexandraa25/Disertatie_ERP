namespace ERPSystem.Modules.Authentification.Models
{
    public class ConfirmLoginRequest
    {
        public string TempToken { get; set; }
        public string Code { get; set; }
    }
}
