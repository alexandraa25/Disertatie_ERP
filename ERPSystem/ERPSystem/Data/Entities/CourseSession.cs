using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Data.Entities;

public class CourseSession
{
    public int Id { get; set; }

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int? Capacity { get; set; } // null = nelimitat

    public string Title { get; set; }

    public CourseFeeType FeeType { get; set; }
    public decimal Fee { get; set; }

    // 🔥 doar pentru FIXED
    public int? TotalSessions { get; set; }

    [Range(1, 7)]
    public int DayOfWeek { get; set; }

    public string TeacherUserId { get; set; }
    public ApplicationUser Teacher { get; set; }  // 🔥 ASTA LIPSEȘTE
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
