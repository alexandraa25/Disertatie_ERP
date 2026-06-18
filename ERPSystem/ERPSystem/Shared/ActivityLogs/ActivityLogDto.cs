namespace ERPSystem.Shared.ActivityLogs
{
    public class ActivityLogDto
    {
        public string EntityType { get; set; } = null!;
        public string EntityId { get; set; } = string.Empty;

        public string Action { get; set; } = null!;
        public DateTime CreatedAtUtc { get; set; }

        public string? Description { get; set; }
        public string? PerformedBy { get; set; }
        public string? PerformedByName { get; set; }

        public Dictionary<string, object?> OldValues { get; set; } = new();
        public Dictionary<string, object?> NewValues { get; set; } = new();
    }
}
