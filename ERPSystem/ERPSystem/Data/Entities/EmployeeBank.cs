namespace ERPSystem.Data.Entities
{
    public class EmployeeBank
    {
        public Guid Id { get; set; }

        public Guid EmployeeId { get; set; }
        private Employee? _employee;
        public Employee Employee
        {
            get => _employee ?? throw new InvalidOperationException("Navigation 'Employee' has not been loaded.");
            set => _employee = value;
        }

        public string IBAN { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
    }
}
