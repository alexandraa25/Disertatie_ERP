namespace ERPSystem.Data.Entities
{
    public class Notification
    {
        public int Id { get; set; }

        public string UserId { get; set; } = default!;

        public string EventType { get; set; } = default!;

        public string Title { get; set; } = default!;

        public string Message { get; set; } = default!;

        public string Type { get; set; } = "Info";

        public string? Link { get; set; }

        public string? EntityType { get; set; }

        public string? EntityId { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime? SeenAt { get; set; }

        public DateTime? ReadAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
