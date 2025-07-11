using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using InfertilityTreatment.Entity.Common;

namespace InfertilityTreatment.Entity.Entities
{
    public class Customer : BaseEntity
    {
        [Required]
        public int UserId { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(200)]
        public string? EmergencyContactName { get; set; }

        [MaxLength(20)]
        public string? EmergencyContactPhone { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? MedicalHistory { get; set; } // JSON format

        [MaxLength(50)]
        public string? MaritalStatus { get; set; }

        [MaxLength(200)]
        public string? Occupation { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;

        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public virtual ICollection<TreatmentCycle> TreatmentCycles { get; set; } = new List<TreatmentCycle>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    }
}