using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Entity.DTOs.TreatmentPhase
{
    public class PhaseResponseDto
    {
        public int Id { get; set; }
        public int CycleId { get; set; }
        public string PhaseName { get; set; } = string.Empty;
        public int PhaseOrder { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string? Instructions { get; set; }
        public string? Notes { get; set; }
    }
}
