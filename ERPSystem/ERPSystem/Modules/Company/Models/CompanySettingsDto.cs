namespace ERPSystem.Modules.Company.Models
{
    public class CompanySettingsDto
    {
        public string Name { get; set; }

        public string CUI { get; set; }

        public string RegistrationNumber { get; set; }

        public string Address { get; set; }

        public string IBAN { get; set; }

        public string Bank { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }
        public string? LogoPath { get; set; }

      
        public string? SignatureImage { get; set; }
    }
}
