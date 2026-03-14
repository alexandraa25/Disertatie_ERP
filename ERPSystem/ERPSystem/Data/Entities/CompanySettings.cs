namespace ERPSystem.Data.Entities
{
    public class CompanySettings
    {
        public int Id { get; set; }

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

        public DateTime UpdatedAtUtc { get; set; }
    }
}
