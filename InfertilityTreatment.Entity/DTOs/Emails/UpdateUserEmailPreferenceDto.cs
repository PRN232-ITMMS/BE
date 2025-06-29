using InfertilityTreatment.Entity.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Entity.DTOs.Emails
{
    public class UpdateUserEmailPreferenceDto
    {
        [Required(ErrorMessage = "ID tùy chọn email là bắt buộc.")]
        [Range(1, int.MaxValue, ErrorMessage = "ID tùy chọn email phải là một số dương.")]
        public int Id { get; set; }

        [Required(ErrorMessage = "User ID là bắt buộc.")]
        [Range(1, int.MaxValue, ErrorMessage = "User ID phải là một số dương.")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Loại thông báo là bắt buộc.")]
        public NotificationType NotificationType { get; set; }

        public bool IsEnabled { get; set; }
    }
}
