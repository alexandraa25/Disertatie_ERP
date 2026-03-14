namespace ERPSystem.Data.Entities
{
    public class ContractSigningToken
    {
        public int Id { get; set; }

        public int ContractId { get; set; }
        public StudentContract Contract { get; set; }

        public string Token { get; set; }

        public DateTime ExpiresAtUtc { get; set; }

        public bool IsUsed { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
