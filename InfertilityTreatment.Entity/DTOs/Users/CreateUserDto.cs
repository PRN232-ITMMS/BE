using System.ComponentModel.DataAnnotations;
using InfertilityTreatment.Entity.Enums;

namespace InfertilityTreatment.Entity.DTOs.Users
{
    public class CreateUserDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
        public string FullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number format")]
        public string? PhoneNumber { get; set; }

        public Gender? Gender { get; set; }

        [Required(ErrorMessage = "Role is required")]
        [EnumDataType(typeof(UserRole), ErrorMessage = "Role must be Doctor or Manager")]
        public UserRole Role { get; set; }

        // Doctor specific fields
        public string? LicenseNumber { get; set; }
        public string? Specialization { get; set; }
        public int? YearsOfExperience { get; set; }
        public string? Education { get; set; }
        public string? Biography { get; set; }
        public decimal? ConsultationFee { get; set; }
        public decimal? SuccessRate { get; set; }
    }
}
