﻿using InfertilityTreatment.Entity.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Entity.DTOs.Users
{
    public class UpdateProfileDto
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public Gender? Gender { get; set; }
    }
}
