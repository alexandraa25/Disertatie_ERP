using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Data.Entities;

public class CourseSession
{
    public int Id { get; set; }

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int? Capacity { get; set; }

    public string Title { get; set; } = string.Empty;

    public CourseFeeType FeeType { get; set; }
    public decimal Fee { get; set; }


    public int? TotalSessions { get; set; }

    [Range(1, 7)]
    public int DayOfWeek { get; set; }

    public string TeacherUserId { get; set; } = string.Empty;
    private ApplicationUser? _teacher;
        public ApplicationUser Teacher
        {
            get => _teacher ?? throw new InvalidOperationException("Navigation 'Teacher' has not been loaded.");
            set => _teacher = value;
        }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<CourseEnrollment> Enrollments { get; set; }
      = new List<CourseEnrollment>();
}

public enum CourseFeeType
{
    FixedPackage = 1,  // ședințe fixe + preț total
    Monthly = 2        // abonament lunar
}
