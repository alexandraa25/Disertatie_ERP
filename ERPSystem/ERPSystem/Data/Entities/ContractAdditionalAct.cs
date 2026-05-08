using ERPSystem.Utils.Enums;

namespace ERPSystem.Data.Entities
{
    public class ContractAdditionalAct
    {
        public int Id { get; set; }

        public int ContractId { get; set; }
        public StudentContract Contract { get; set; }

        public string ActNumber { get; set; } = null!;
        public string Description { get; set; } = null!;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? AppliedAtUtc { get; set; }

        public AdditionalActStatus Status { get; set; }

        public string? Body { get; set; }

        public string? ClientSignature { get; set; }
        public string? AdminSignature { get; set; }

        public DateTime? ClientSignedAtUtc { get; set; }
        public DateTime? AdminSignedAtUtc { get; set; }

        public string? PdfPath { get; set; }
        public ICollection<ContractAdditionalActItem> Items { get; set; } = new List<ContractAdditionalActItem>();
    }
}
