using InfertilityTreatment.Entity.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InfertilityTreatment.Entity.Enums;

namespace InfertilityTreatment.Entity.Entities
{
    public class TestResult : BaseEntity
    {
        [Required]
        public int CycleId { get; set; }

        [Required]
        public TestResultType TestType { get; set; }

        [Required]
        public int AppointmentId { get; set; }

        [Required]
        public DateTime TestDate { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? Results { get; set; }

        [Column(TypeName = "nvarchar(100)")]
        public string? ReferenceRange { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public TestResultStatus Status { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? DoctorNotes { get; set; }

        [Required]
        public bool IsCritical { get; set; } = false;


        [ForeignKey(nameof(AppointmentId))]
        public virtual Appointment? Appointment { get; set; } = null!;

        // Navigation
        [ForeignKey(nameof(CycleId))]
        public virtual TreatmentCycle TreatmentCycle { get; set; } = null!;
    }
}
