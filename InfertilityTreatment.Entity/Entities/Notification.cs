using InfertilityTreatment.Entity.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InfertilityTreatment.Entity.Enums;

namespace InfertilityTreatment.Entity.Entities
{
    [Table("Notifications")]
    public class Notification : BaseEntity
    {
        [Column("UserId")]
        public int UserId { get; set; }

        [Required]
        [Column("Title")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column("Message", TypeName = "nvarchar(max)")]
        public string Message { get; set; } = string.Empty;

        [Required]
        [Column("Type")]
        [MaxLength(50)]
        public NotificationType Type { get; set; }

        [Column("IsRead")]
        public bool IsRead { get; set; } = false;

        [Column("RelatedEntityType")]
        [MaxLength(100)]
        public string? RelatedEntityType { get; set; }

        [Column("RelatedEntityId")]
        public int? RelatedEntityId { get; set; }

        [Column("ScheduledAt")]
        public DateTime? ScheduledAt { get; set; }

        [Column("SentAt")]
        public DateTime? SentAt { get; set; }

        [Column("EmailStatus")]
        [MaxLength(20)]
        public EmailStatus? EmailStatus { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;
    }
}
