using InfertilityTreatment.Entity.Common;
using InfertilityTreatment.Entity.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Entity.Entities
{
    [Table("UserEmailPreferences")]
    public class UserEmailPreference : BaseEntity
    {
        [Column("UserId")]
        public int UserId { get; set; }

        [Required]
        [Column("NotificationType")]
        public NotificationType NotificationType { get; set; } 

        [Column("IsEnabled")]
        public bool IsEnabled { get; set; } = true;

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;
    }
}
