using InfertilityTreatment.Entity.Entities;
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
    public class CreateCycleDto
    {
        [Required]
        public int CustomerId { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required]
        public int PackageId { get; set; }

        public int CycleNumber { get; set; } = 1;
        public DateTime? StartDate { get; set; }

        public DateTime? ExpectedEndDate { get; set; }

        public DateTime? ActualEndDate { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal? TotalCost { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? Notes { get; set; }

        public bool? IsSuccessful { get; set; } 

    }
}
