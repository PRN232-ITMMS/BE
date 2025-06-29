using InfertilityTreatment.Entity.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Entity.Entities
{
    public class TreatmentPhase : BaseEntity
    {
        public int CycleId { get; set; }
        [Required]
        [MaxLength(200)]
        public string PhaseName { get; set; } = string.Empty;
        public int PhaseOrder { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        [Column(TypeName = "decimal(12,2)")]
        public decimal Cost { get; set; }
        public string? Instructions { get; set; }
        public string? Notes { get; set; }

        [ForeignKey(nameof(CycleId))]
        public TreatmentCycle? Cycle { get; set; }

        public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    }
}
