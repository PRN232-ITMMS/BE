using InfertilityTreatment.Entity.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Entity.DTOs.Notifications
{
    public class CreateNotificationDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        [Required]
        public NotificationType Type { get; set; }

        public int? RelatedEntityId { get; set; }

        [MaxLength(100)]
        public string? RelatedEntityType { get; set; }

        public DateTime? ScheduledAt { get; set; }

        public bool SendEmail { get; set; } = false;
        public string? EmailSubject { get; set; }
        public string? EmailTemplateName { get; set; }
        public Dictionary<string, string>? EmailTemplateData { get; set; }
    }
}
