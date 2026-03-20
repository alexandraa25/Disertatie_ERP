namespace ERPSystem.Data.Entities
{
    public class ContractInstallment
    {
        public int Id { get; set; }

        public int ContractId { get; set; }
        public StudentContract Contract { get; set; }

        public DateTime DueDate { get; set; }

        public decimal Amount { get; set; }

        public bool IsPaid { get; set; }

       
    }
}
