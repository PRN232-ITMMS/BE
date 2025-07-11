using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using InfertilityTreatment.Entity.Common;
using InfertilityTreatment.Entity.Enums;

namespace InfertilityTreatment.Entity.Entities
{
    public class TreatmentCycle : BaseEntity
    {
        [Required]
        public int CustomerId { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required]
        public int PackageId { get; set; }

        public int CycleNumber { get; set; } = 1;

        [Required]
        public CycleStatus Status { get; set; } = CycleStatus.Created;

        public DateTime? StartDate { get; set; }

        public DateTime? ExpectedEndDate { get; set; }

        public DateTime? ActualEndDate { get; set; }

        // New properties for enhanced initialization
        public DateTime? ActualStartDate { get; set; }

        public DateTime? EstimatedCompletionDate { get; set; }

        [Column(TypeName = "nvarchar(2000)")]
        public string? TreatmentPlan { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? SpecialInstructions { get; set; }

        [Column(TypeName = "nvarchar(1000)")]
        public string? DoctorNotes { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal? TotalCost { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? Notes { get; set; }

        public bool? IsSuccessful { get; set; } // Null until completed

        // Navigation Properties
        [ForeignKey(nameof(CustomerId))]
        public virtual Customer Customer { get; set; } = null!;

        [ForeignKey(nameof(DoctorId))]
        public virtual Doctor Doctor { get; set; } = null!;

        [ForeignKey(nameof(PackageId))]
        public virtual TreatmentPackage TreatmentPackage { get; set; } = null!;

        public virtual ICollection<TreatmentPhase> TreatmentPhases { get; set; } = new List<TreatmentPhase>();
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public virtual ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}