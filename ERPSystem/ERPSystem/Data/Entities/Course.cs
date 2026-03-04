using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Data.Entities;

public class Course
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = "";

    [MaxLength(500)]
    public string? Description { get; set; }


    public bool IsActive { get; set; } = true;
   

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<CourseSession> Sessions { get; set; } = new();
    public List<CourseEnrollment> Enrollments { get; set; } = new();
}
