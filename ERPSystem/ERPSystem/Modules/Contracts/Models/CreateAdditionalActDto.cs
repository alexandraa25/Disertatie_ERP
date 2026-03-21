using ERPSystem.Utils.Enums;

namespace ERPSystem.Modules.Contracts.Models
{
    public class CreateAdditionalActDto
    {
        public List<AdditionalActType> Types { get; set; } = new();

        public List<int> CourseSessionIds { get; set; }

        public int? StudentId { get; set; }

        public DateTime? NewEndDate { get; set; }

        public decimal? NewPrice { get; set; }

        public string Description { get; set; }
    }

    public record AdditionalActDto(
    int Id,
    string ActNumber,
    string Type,
    string Description,

    DateTime CreatedAtUtc
);
}


