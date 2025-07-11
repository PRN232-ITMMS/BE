using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using InfertilityTreatment.Entity.Common;
using InfertilityTreatment.Entity.Enums;

namespace InfertilityTreatment.Entity.Entities
{
    public class Appointment : BaseEntity
    {
   
        public int CustomerId { get; set; }

        public int? CycleId { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required]
        public AppointmentType AppointmentType { get; set; }

        public DateTime ScheduledDateTime { get; set; }

        [Required]
        public AppointmentStatus Status { get; set; }

        [Required]
        public int DoctorScheduleId { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? Notes { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? Results { get; set; }

        // Navigation
        [ForeignKey(nameof(CycleId))]
        public virtual TreatmentCycle? TreatmentCycle { get; set; } = null!;

        [ForeignKey(nameof(CustomerId))]
        public virtual Customer? Customer { get; set; } = null!;

        [ForeignKey(nameof(DoctorId))]
        public virtual Doctor Doctor { get; set; } = null!;

        [ForeignKey(nameof(DoctorScheduleId))]
        public virtual DoctorSchedule DoctorSchedule { get; set; } = null!;

        public virtual ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
    }
}