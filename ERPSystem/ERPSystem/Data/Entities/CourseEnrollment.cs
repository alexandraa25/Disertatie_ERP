using ERPSystem.Modules.Student.Models;

namespace ERPSystem.Data.Entities;

public class CourseEnrollment
{   public int Id {  get; set; }

    public int CourseId { get; set; }
    private Course? _course;
        public Course Course
        {
            get => _course ?? throw new InvalidOperationException("Navigation 'Course' has not been loaded.");
            set => _course = value;
        }
    public int CourseSessionId { get; set; }
    public CourseSession Session { get; set; } = default!;

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public DateTime EnrolledAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAtUtc { get; set; }
    public bool IsActive { get; set; } = true;

    public int? ContractId { get; set; }
    public StudentContract Contract { get; set; } = null!;
}
