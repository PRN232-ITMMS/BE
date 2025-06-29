using InfertilityTreatment.Entity.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InfertilityTreatment.Entity.Entities
{
    public class DoctorSchedule : BaseEntity
    {
        [Required]
        public int DoctorId { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        // Navigation
        [ForeignKey(nameof(DoctorId))]
        public virtual Doctor Doctor { get; set; } = null!;
    }
}
