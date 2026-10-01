namespace ERPSystem.Modules.MarketingCampaign.Models
{
    public class SendCampaignNewsletterRequest
    {
        public int CampaignId { get; set; }

        public string RecipientMode { get; set; } = "active";

        public List<int> StudentIds { get; set; } = new();

        public string Subject { get; set; } = string.Empty;

        public string HtmlContent { get; set; } = string.Empty;
    }
}
