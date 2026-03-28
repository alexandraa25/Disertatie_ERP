namespace ERPSystem.Data.Entities
{
    public class ContractCourse
    {
        public int Id { get; set; }

        public int ContractId { get; set; }
        public StudentContract Contract { get; set; } = null!;

        public int CourseSessionId { get; set; }
        public CourseSession CourseSession { get; set; } = null!;

        // Snapshot important
        public string CourseNameSnapshot { get; set; } = default!;
        public string SessionNameSnapshot { get; set; } = default!;
        public decimal PriceSnapshot { get; set; }

        public CourseFeeType FeeType { get; set; } // 🔥 ADD
    }
}
