namespace ERPSystem.Data.Entities
{
    public class EmployeeContact
    {
        public Guid Id { get; set; }

        public Guid EmployeeId { get; set; }
        private Employee? _employee;
        public Employee Employee
        {
            get => _employee ?? throw new InvalidOperationException("Navigation 'Employee' has not been loaded.");
            set => _employee = value;
        }

        public string? PhoneNumber { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
    }
}
