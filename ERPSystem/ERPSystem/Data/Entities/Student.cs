using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPSystem.Data.Entities
{
    public class Student
    {
        public int Id { get; set; }

        [Required, MaxLength(120)]
        public string FullName { get; set; } = "";

        [MaxLength(80)]
        public string? FirstName { get; set; }

        [MaxLength(80)]
        public string? LastName { get; set; }

        [MaxLength(120)]
        public string? Email { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(250)]
        public string? Address { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public ICollection<StudentGuardian> StudentGuardians { get; set; }
            = new List<StudentGuardian>();

        public ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();

        [NotMapped]
        public int? Age
        {
            get
            {
                if (!DateOfBirth.HasValue) return null;

                var today = DateTime.Today;
                var age = today.Year - DateOfBirth.Value.Year;

                if (DateOfBirth.Value.Date > today.AddYears(-age))
                    age--;

                return age;
            }
        }

        [NotMapped]
        public bool IsMinor => Age.HasValue && Age < 18;
    }
}