public class StudentEvaluation
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public int CourseSessionId { get; set; }

    public string TeacherUserId { get; set; }

    public int Rating { get; set; }

    public int? AttendanceScore { get; set; }

    public int? BehaviorScore { get; set; }

    public int? ProgressScore { get; set; }

    public string? Comment { get; set; }

    public string? Sentiment { get; set; }

    public double? SentimentScore { get; set; }

    public string? Keywords { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? AnalyzedAt { get; set; }
}