namespace ERPSystem.Modules.Contracts.Models
{
    public class SignContractDto
    {
        public string Token { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
    }

    public class AdminSignContractDto
    {
        public string Signature { get; set; } = string.Empty;
    }
}
