using InfertilityTreatment.Entity.Enums;

namespace InfertilityTreatment.Entity.DTOs.Users
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public Gender? Gender { get; set; }
        public UserRole Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        
        // Navigation properties
        public CustomerProfileDto? Customer { get; set; }
        public DoctorProfileDto? Doctor { get; set; }
    }
}
