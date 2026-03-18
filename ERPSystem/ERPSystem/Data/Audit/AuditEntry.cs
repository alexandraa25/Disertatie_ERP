using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ERPSystem.Data.Audit
{
    public class AuditEntry
    {
        public AuditEntry(EntityEntry entry)
        {
            Entry = entry;
        }

        public EntityEntry Entry { get; }

        public string EntityType { get; set; } = null!;
        public string Action { get; set; } = null!;

        public Dictionary<string, object?> OldValues { get; } = new();
        public Dictionary<string, object?> NewValues { get; } = new();
        public Dictionary<string, object?> KeyValues { get; } = new();

        public bool HasChanges => OldValues.Count > 0 || NewValues.Count > 0;

        public int GetEntityId()
        {
            return KeyValues.Values.FirstOrDefault() is int id ? id : 0;
        }

        public void UpdateKeyValues()
        {
            foreach (var prop in Entry.Properties)
            {
                if (prop.Metadata.IsPrimaryKey())
                {
                    KeyValues[prop.Metadata.Name] = prop.CurrentValue!;
                }
            }
        }
    }
}