namespace ERPSystem.Modules.Students.Models
{
    public class SessionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;

        public int CourseId { get; set; }
        public string CourseName { get; set; } = null!;

        public string TeacherName { get; set; } = null!;

        public DayOfWeek DayOfWeek { get; set; }
        public string StartTime { get; set; } = null!;
        public string EndTime { get; set; } = null!;

        public decimal Fee { get; set; }

        public bool IsActive { get; set; }
    }
}
