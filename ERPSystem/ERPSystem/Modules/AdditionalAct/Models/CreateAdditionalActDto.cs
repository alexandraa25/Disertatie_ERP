using ERPSystem.Modules.Contracts.Models;
using ERPSystem.Utils.Enums;

namespace ERPSystem.Modules.AdditionalAct.Models
{
    public class CreateAdditionalActDto
    {
        public List<AdditionalActType> Types { get; set; } = new();

        public List<int> AddCourseSessionIds { get; set; } = new();

        public List<int> RemoveCourseSessionIds { get; set; } = new();

        public DateTime? NewEndDate { get; set; }

        public decimal? PriceAdjustment { get; set; }

        public string? Description { get; set; }

        public List<PriceAdjustmentDto> PriceAdjustments { get; set; } = new();
    }

    public class PriceAdjustmentDto
    {
        public int CourseSessionId { get; set; }

        public decimal Amount { get; set; }
    }

    public class AdditionalActDetailsDto
    {
        public int Id { get; set; }
        public string ActNumber { get; set; } = default!;
        public string Status { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string Body { get; set; } = default!;
        public DateTime CreatedAtUtc { get; set; }
        public int ContractId { get; set; }

        public List<ContractPartyDto> Parties { get; set; } = new();
        public List<AdditionalActItemDto> Items { get; set; } = new();
    }

    public class AdditionalActItemDto
    {
        public string Type { get; set; } = default!;
        public int? CourseSessionId { get; set; }
        public string? NewValue { get; set; }
    }

    public class UpdateAdditionalActBodyDto
    {
        public string Body { get; set; } = default!;
    }
}