namespace InfertilityTreatment.Entity.DTOs.Users
{
    public class CustomerProfileDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? Address { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? MedicalHistory { get; set; }
        public string? MaritalStatus { get; set; }
        public string? Occupation { get; set; }
    }
    
    public class MedicalHistoryDTO
    {
        public string MedicalHistory { get; set; } = string.Empty;
    }
}
