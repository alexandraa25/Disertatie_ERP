using ERPSystem.Modules.Contracts.Models;
using ERPSystem.Utils.Enums;

namespace ERPSystem.Modules.AdditionalAct.Models
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

    public record AdditionalActDetailsDto(
    int Id,
    string ActNumber,
    string Status,
    string Description,
    string Body,
    DateTime CreatedAtUtc,
    int ContractId,

    List<ContractPartyDto> Parties,
    List<AdditionalActItemDto> Items
);

    public record AdditionalActItemDto(
    string Type,
    int? CourseSessionId,
    string? NewValue
);
    public record UpdateAdditionalActBodyDto(
     string Body
 );

}


