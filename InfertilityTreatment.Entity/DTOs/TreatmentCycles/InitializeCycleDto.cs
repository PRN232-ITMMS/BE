using System.ComponentModel.DataAnnotations;
using InfertilityTreatment.Entity.Enums;

namespace InfertilityTreatment.Entity.DTOs.TreatmentCycles
{
    public class InitializeCycleDto
    {
        [Required]
        public DateTime StartDate { get; set; }
        
        [Required]
        [StringLength(2000)]
        public string TreatmentPlan { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? SpecialInstructions { get; set; }
        
        public DateTime? EstimatedCompletionDate { get; set; }
        
        
        public bool AutoGeneratePhases { get; set; } = true;
        
        public bool AutoScheduleAppointments { get; set; } = true;
    }
}
