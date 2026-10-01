namespace ERPSystem.Data.Entities
{
    public class ContractTemplate
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
