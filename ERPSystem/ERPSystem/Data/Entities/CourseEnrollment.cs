using ERPSystem.Modules.Student.Models;

namespace ERPSystem.Data.Entities;

public class CourseEnrollment
{   public int Id {  get; set; }

    public int CourseId { get; set; }
    public Course Course { get; set; }
    public int CourseSessionId { get; set; }
    public CourseSession Session { get; set; } = default!;

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public DateTime EnrolledAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
