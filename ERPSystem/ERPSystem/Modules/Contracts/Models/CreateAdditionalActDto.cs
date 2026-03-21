using ERPSystem.Utils.Enums;

namespace ERPSystem.Modules.Contracts.Models
{
    public class CreateAdditionalActDto
    {
        public AdditionalActType Type { get; set; }
        public string Description { get; set; } = "";

        public List<int>? CourseSessionIds { get; set; }
        public List<int>? StudentIds { get; set; }

        public DateTime? NewEndDate { get; set; }
        public decimal? PriceDifference { get; set; }
    }

    public record AdditionalActDto(
    int Id,
    string ActNumber,
    string Type,
    string Description,
    string Status,
    DateTime CreatedAtUtc
);
}


