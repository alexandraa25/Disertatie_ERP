namespace ERPSystem.Data.Entities
{
    public class SigningToken
    {
        public int Id { get; set; }

        public string Token { get; set; } = null!;

        public DateTime CreatedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }

        public bool IsUsed { get; set; }

        
        public SigningEntityType EntityType { get; set; }

        
        public int EntityId { get; set; }
    }

    public enum SigningEntityType
    {
        Contract = 1,
        AdditionalAct = 2
    }
}
