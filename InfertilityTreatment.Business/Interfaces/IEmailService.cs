using InfertilityTreatment.Entity.DTOs.Email;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Business.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
        Task<bool> TestConfigurationAsync();
        Task<EmailResponseDto> SendWelcomeEmailAsync(SendWelcomeEmailDto dto);
        Task<EmailResponseDto> SendAppointmentConfirmationAsync(SendAppointmentConfirmationDto dto);
        Task<EmailResponseDto> SendAppointmentReminderAsync(SendReminderDto dto);
        Task<EmailResponseDto> SendTestResultNotificationAsync(SendTestResultDto dto);
        Task<EmailResponseDto> SendTreatmentPhaseUpdateAsync(SendTreatmentPhaseUpdateDto dto);
        Task<EmailResponseDto> SendBulkNotificationAsync(BulkEmailDto dto);
    }
}
