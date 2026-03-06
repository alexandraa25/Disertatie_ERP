using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Data.Entities;

public class CourseSession
{
    public int Id { get; set; }

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int? Capacity { get; set; } // null = nelimitat

    public string Title { get; set; }
    public decimal Fee { get; set; }

    // 1=Monday ... 7=Sunday
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
