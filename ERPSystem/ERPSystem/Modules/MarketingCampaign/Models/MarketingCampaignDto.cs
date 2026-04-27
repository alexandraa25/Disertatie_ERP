using ERPSystem.Data.Entities;
using ERPSystem.Utils.Enums;

namespace ERPSystem.Modules.MarketingCampaign.Models
{
    public class MarketingCampaignDto
    {
        public int? Id { get; set; }

        public string Name { get; set; } = default!;
        public string? Description { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        public DiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }

        public DiscountScope DiscountScope { get; set; }

        public int? CourseId { get; set; }
        public int? CourseSessionId { get; set; }

        public List<int> CourseSessionIds { get; set; } = new();


    }

    public class MarketingCampaignQuery
    {
        public string? Search { get; set; }

        public bool? IsActive { get; set; }

        public int? CourseId { get; set; }
        public int? CourseSessionId { get; set; }

        public string? PeriodStatus { get; set; }

        public DiscountScope? Scope { get; set; }

        public string? SortBy { get; set; } // name, startDate, endDate
        public bool Desc { get; set; } = true;

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public record AvailableCampaignsRequest(List<int> CourseSessionIds);


    public class ToggleCampaignRequest
    {
        public DateTime? EndDate { get; set; }
    }
}
