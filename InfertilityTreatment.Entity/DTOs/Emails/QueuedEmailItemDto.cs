using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Entity.DTOs.Emails
{
    public class QueuedEmailItemDto
    {
        public EmailDto EmailDto { get; set; } = new EmailDto();
        public string? TemplateName { get; set; }
        public Dictionary<string, string>? TemplateData { get; set; }
        public int NotificationId { get; set; } 
        public int RetryCount { get; set; } = 0;
    }
}
