using InfertilityTreatment.Entity.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Entity.DTOs.Results
{
    public class TestResultDto
    {
        public int Id { get; set; }
        public int CycleId { get; set; }
        public int AppointmentId { get; set; }
        public string TestType { get; set; } = string.Empty;
        public DateTime TestDate { get; set; }
        public string? Results { get; set; }
        public string? ReferenceRange { get; set; }
        public TestResultStatus Status { get; set; }
        public string? DoctorNotes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsCritical { get; set; }
    }
}
