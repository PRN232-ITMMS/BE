using InfertilityTreatment.Entity.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Entity.DTOs.Results
{
    public class CreateTestResultDto
    {
        public int CycleId { get; set; }
        public TestResultType TestType { get; set; }
        public int AppointmentId { get; set; }
        public DateTime TestDate { get; set; }
        public string? Results { get; set; }
        [MaxLength(100)]
        public string? ReferenceRange { get; set; }
        public TestResultStatus Status { get; set; }
        public string? DoctorNotes { get; set; }
        public bool IsCritical { get; set; } = false;
    }
}
