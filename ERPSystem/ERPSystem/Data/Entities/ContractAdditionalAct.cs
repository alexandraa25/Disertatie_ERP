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

        public AdditionalActStatus Status { get; set; }

        public string? Body { get; set; }

        public bool IsSignedByStudent { get; set; }
        public bool IsSignedByCompany { get; set; }

        public DateTime? StudentSignedAtUtc { get; set; }
        public DateTime? CompanySignedAtUtc { get; set; }

        
        public ICollection<ContractAdditionalActItem> Items { get; set; } = new List<ContractAdditionalActItem>();
    }
}
