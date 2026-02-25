using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Data.Entities
{
    public class Guardian
    {
        public int Id { get; set; }

        [Required, MaxLength(80)]
        public string FirstName { get; set; } = "";

        [Required, MaxLength(80)]
        public string LastName { get; set; } = "";

        [Required, MaxLength(120)]
        public string Email { get; set; } = "";

        [Required, MaxLength(30)]
        public string Phone { get; set; } = "";

        [MaxLength(250)]
        public string? Address { get; set; }

        [MaxLength(50)]
        public string? PersonalIdentificationNumber { get; set; }

        public bool HasSignedConsent { get; set; } = false;
        public DateTime? ConsentSignedAtUtc { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public ICollection<StudentGuardian> StudentGuardians { get; set; }
            = new List<StudentGuardian>();
    }
}
