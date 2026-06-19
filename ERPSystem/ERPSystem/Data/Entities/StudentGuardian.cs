using ERPSystem.Modules.Student.Models;
using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Data.Entities
{
    public class StudentGuardian
    {
        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public int GuardianId { get; set; }
        public Guardian Guardian { get; set; } = null!;

        [Required, MaxLength(50)]
        public string RelationshipType { get; set; } = ""; 

        public bool IsPrimaryContact { get; set; } = false;
    }
}
