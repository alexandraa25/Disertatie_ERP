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

        public decimal? PriceDifference { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = "Draft"; // Draft, Finalized

        public string? Body { get; set; }

        public bool IsSigned { get; set; }

        public AdditionalActType Type { get; set; }

        public ICollection<ContractAdditionalAct> AdditionalActs { get; set; }
    }
}
