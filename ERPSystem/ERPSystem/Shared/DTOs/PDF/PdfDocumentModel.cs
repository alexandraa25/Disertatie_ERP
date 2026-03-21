namespace ERPSystem.Shared.DTOs.PDF
{
    public class PdfDocumentModel
    {
        public string Title { get; set; } = null!;
        public string Number { get; set; } = null!;
        public string Body { get; set; } = null!;

        public string CompanyName { get; set; } = null!;
        public string BeneficiaryName { get; set; } = null!;

        public string? AdminSignature { get; set; }
        public string? ClientSignature { get; set; }

        public DateTime? AdminSignedAt { get; set; }
        public DateTime? ClientSignedAt { get; set; }
    }
}
