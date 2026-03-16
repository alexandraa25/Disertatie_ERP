namespace ERPSystem.Data.Entities
{
    public class ContractTemplate
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Body { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
