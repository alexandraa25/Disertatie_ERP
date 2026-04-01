namespace ERPSystem.Data.Entities
{
    public class PublicHoliday
    {
        public Guid Id { get; set; }

        public DateTime Date { get; set; }

        public string Name { get; set; }

        public string Country { get; set; } = "RO";
    }
}
