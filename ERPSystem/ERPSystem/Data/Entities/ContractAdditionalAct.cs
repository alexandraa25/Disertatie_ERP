using ERPSystem.Utils.Enums;

namespace ERPSystem.Data.Entities
{
    public class ContractAdditionalAct
    {
        public int Id { get; set; }

        public int ContractId { get; set; }
        private StudentContract? _contract;
        public StudentContract Contract
        {
            get => _contract ?? throw new InvalidOperationException("Navigation 'Contract' has not been loaded.");
            set => _contract = value;
        }

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
