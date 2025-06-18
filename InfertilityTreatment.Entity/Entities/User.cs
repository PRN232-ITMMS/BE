using System.ComponentModel.DataAnnotations;
using System.Numerics;
using InfertilityTreatment.Entity.Common;
using InfertilityTreatment.Entity.Enums;

namespace InfertilityTreatment.Entity.Entities
{
    public class User : BaseEntity
    {
        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        public Gender? Gender { get; set; }

        [Required]
        public UserRole Role { get; set; }

        // Navigation Properties
        public virtual Customer? Customer { get; set; }
        public virtual Doctor? Doctor { get; set; }
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        //public virtual ICollection<BlogPost> BlogPosts { get; set; } = new List<BlogPost>();
        //public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}