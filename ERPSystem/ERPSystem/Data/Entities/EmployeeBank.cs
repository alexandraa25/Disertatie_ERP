namespace ERPSystem.Data.Entities
{
    public class EmployeeBank
    {
        public Guid Id { get; set; }

        public Guid EmployeeId { get; set; }
        public Employee Employee { get; set; }

        public string IBAN { get; set; }
        public string BankName { get; set; }
    }
}
