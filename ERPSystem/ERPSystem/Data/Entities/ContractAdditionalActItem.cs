using ERPSystem.Utils.Enums;

namespace ERPSystem.Data.Entities
{
    public class ContractAdditionalActItem
    {
        public int Id { get; set; }

        public int ActId { get; set; }
        private ContractAdditionalAct? _act;
        public ContractAdditionalAct Act
        {
            get => _act ?? throw new InvalidOperationException("Navigation 'Act' has not been loaded.");
            set => _act = value;
        }

        public AdditionalActType Type { get; set; }

        public int? CourseSessionId { get; set; }
        public int? StudentId { get; set; }

        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
    }
}
