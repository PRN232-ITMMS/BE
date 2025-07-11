using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Entity.DTOs.DoctorSchedules
{
    public class CreateDoctorScheduleDto
    {

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

    }
}
