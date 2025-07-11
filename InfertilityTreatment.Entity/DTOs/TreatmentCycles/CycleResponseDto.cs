using InfertilityTreatment.Entity.DTOs.TreatmentPhase;
using InfertilityTreatment.Entity.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Entity.DTOs.TreatmentCycles
{
    public class CycleResponseDto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;
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

        [Column(TypeName = "decimal(12,2)")]
        public decimal? TotalCost { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? Notes { get; set; }

        public bool? IsSuccessful { get; set; } // Null until completed
        public List<PhaseResponseDto> Phase { get; set; } = new List<PhaseResponseDto>();
    }
}
