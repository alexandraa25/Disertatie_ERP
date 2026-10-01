namespace ERPSystem.Data.Entities
{
    public class CompanySettings
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string CUI { get; set; } = string.Empty;

        public string RegistrationNumber { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string IBAN { get; set; } = string.Empty;

        public string Bank { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string? LogoPath { get; set; }

        public string? SignatureImage { get; set; }

        public DateTime UpdatedAtUtc { get; set; }
    }
}
