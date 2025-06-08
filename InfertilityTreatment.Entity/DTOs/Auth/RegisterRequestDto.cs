using System.ComponentModel.DataAnnotations;
using InfertilityTreatment.Entity.Enums;

namespace InfertilityTreatment.Entity.DTOs.Auth
{
    public class RegisterRequestDto
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
        public UserRole Role { get; set; } = UserRole.Customer;

        // Customer specific fields
        public string? Address { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? MaritalStatus { get; set; }
        public string? Occupation { get; set; }
    }
}
