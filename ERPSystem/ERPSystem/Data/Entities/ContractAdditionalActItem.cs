using ERPSystem.Utils.Enums;

namespace ERPSystem.Data.Entities
{
    public class ContractAdditionalActItem
    {
        public int Id { get; set; }

        public int ActId { get; set; }
        public ContractAdditionalAct Act { get; set; }

        public AdditionalActType Type { get; set; }

        public int? CourseSessionId { get; set; }
        public int? StudentId { get; set; }

        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
    }
}
