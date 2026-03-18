namespace ERPSystem.Shared.ActivityLogs
{
    public class ActivityLogDto
    {
        public string EntityType { get; set; } = null!;
        public int EntityId { get; set; }

        public string Action { get; set; } = null!;
        public DateTime CreatedAtUtc { get; set; }

        public string? Description { get; set; }
        public string? PerformedBy { get; set; }

        public Dictionary<string, object?> OldValues { get; set; } = new();
        public Dictionary<string, object?> NewValues { get; set; } = new();
    }
}
