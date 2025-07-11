using InfertilityTreatment.Entity.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Entity.DTOs.Appointments
{
    public class AppointmentResponseDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int? CycleId { get; set; }
        public int DoctorId { get; set; }
        public int DoctorScheduleId { get; set; }
        public AppointmentType AppointmentType { get; set; }
        public DateTime ScheduledDateTime { get; set; }
        public AppointmentStatus Status { get; set; }
        public string Notes { get; set; }
        public string Results { get; set; }
    }
}
