namespace ERPSystem.Data.Entities
{
    public class ActivityLog
    {
        public int Id { get; set; }
        public string EntityType { get; set; } = null!;
        public string EntityId { get; set; } = null!;
        public string Action { get; set; } = null!; 
        public string? Changes { get; set; } 
        public string? Description { get; set; } 
        public string? PerformedBy { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
