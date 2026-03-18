namespace ERPSystem.Data.Entities
{
    public class ActivityLog
    {
        public int Id { get; set; }

        public string EntityType { get; set; } = null!;
        public int EntityId { get; set; }

        public string Action { get; set; } = null!; // Create, Update, Delete

        public string? Changes { get; set; } // JSON (Old/New)

        public string? Description { get; set; } // fallback text (opțional)

        public string? PerformedBy { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
