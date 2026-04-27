namespace ERPSystem.Modules.MarketingCampaign.Models
{
    public class EmailLogsRequest
    {
        public string? Type { get; set; }
        public int? ReferenceId { get; set; }
        public string? Search { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
