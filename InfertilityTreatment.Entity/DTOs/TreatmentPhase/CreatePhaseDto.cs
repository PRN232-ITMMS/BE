using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Entity.DTOs.TreatmentPhase
{
    public class CreatePhaseDto
    {
        [Required(ErrorMessage = "CycleId is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "CycleId must be a positive integer.")]
        public int CycleId { get; set; }

        [Required(ErrorMessage = "PhaseName is required.")]
        [MaxLength(200, ErrorMessage = "PhaseName cannot exceed 200 characters.")]
        public string PhaseName { get; set; } = string.Empty;

        [Required(ErrorMessage = "PhaseOrder is required.")]
        [Range(1, 100, ErrorMessage = "PhaseOrder must be between 1 and 100.")]
        public int PhaseOrder { get; set; }

        [Required(ErrorMessage = "Cost is required.")]
        public decimal Cost { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        [MaxLength(50, ErrorMessage = "Status cannot exceed 50 characters.")]
        public string Status { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [MaxLength(1000, ErrorMessage = "Instructions cannot exceed 1000 characters.")]
        public string? Instructions { get; set; }

        [MaxLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
        public string? Notes { get; set; }
    }
}
