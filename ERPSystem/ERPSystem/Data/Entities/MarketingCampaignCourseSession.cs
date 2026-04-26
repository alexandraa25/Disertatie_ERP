namespace ERPSystem.Data.Entities
{
    public class MarketingCampaignCourseSessions
    {
        public int Id { get; set; }

        public int MarketingCampaignId { get; set; }
        public MarketingCampaign MarketingCampaign { get; set; } = null!;

        public int CourseSessionId { get; set; }
        public CourseSession CourseSession { get; set; } = null!;
    }
}
