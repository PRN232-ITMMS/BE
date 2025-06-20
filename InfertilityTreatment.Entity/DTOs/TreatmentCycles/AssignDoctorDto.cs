using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Entity.DTOs.TreatmentCycles
{
    public class AssignDoctorDto
    {
        [Required]
        public int DoctorId { get; set; }
    }
}
