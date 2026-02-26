using ERPSystem.Utils.Enums;
namespace ERPSystem.Data.Entities
{
    public class ContractParty
    {
        public int Id { get; set; }

        public int ContractId { get; set; }
        public StudentContract Contract { get; set; } = null!;

        public int? StudentId { get; set; }
        public Student? Student { get; set; }

        public int? GuardianId { get; set; }
        public Guardian? Guardian { get; set; }

        public ContractPartyRole Role { get; set; }
    }
}
