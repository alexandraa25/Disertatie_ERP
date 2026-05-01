namespace ERPSystem.Modules.Feedback.Analytics.Models
{
    public class TopicSummaryDto
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percent { get; set; }
    }
}
