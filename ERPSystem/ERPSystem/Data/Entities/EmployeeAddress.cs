namespace ERPSystem.Data.Entities
{
    public class EmployeeAddress
    {
        public Guid Id { get; set; }

        public Guid EmployeeId { get; set; }
        private Employee? _employee;
        public Employee Employee
        {
            get => _employee ?? throw new InvalidOperationException("Navigation 'Employee' has not been loaded.");
            set => _employee = value;
        }

        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
    }
}
