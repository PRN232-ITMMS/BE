using InfertilityTreatment.Business.Interfaces;
using InfertilityTreatment.Business.Templates;
using InfertilityTreatment.Data.Repositories.Interfaces;
using InfertilityTreatment.Entity.DTOs.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Business.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly bool _enableSsl;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger, IUnitOfWork unitOfWork)
        {
            _configuration = configuration;
            _logger = logger;
            _unitOfWork = unitOfWork;

            // Read from SmtpSettings primarily, with EmailSettings as fallback
            _smtpHost = _configuration["SmtpSettings:Host"] ?? _configuration["EmailSettings:SmtpHost"] ?? throw new ArgumentNullException("SmtpSettings:Host not configured.");
            _smtpPort = _configuration.GetValue<int>("SmtpSettings:Port") != 0 ? _configuration.GetValue<int>("SmtpSettings:Port") : _configuration.GetValue<int>("EmailSettings:SmtpPort");
            _smtpUser = _configuration["SmtpSettings:Username"] ?? _configuration["EmailSettings:Username"] ?? throw new ArgumentNullException("SmtpSettings:Username not configured.");
            _smtpPass = _configuration["SmtpSettings:Password"] ?? _configuration["EmailSettings:Password"] ?? throw new ArgumentNullException("SmtpSettings:Password not configured.");
            _fromEmail = _configuration["SmtpSettings:FromEmail"] ?? _configuration["EmailSettings:FromEmail"] ?? throw new ArgumentNullException("SmtpSettings:FromEmail not configured.");
            _fromName = _configuration["SmtpSettings:FromName"] ?? _configuration["EmailSettings:FromName"] ?? "Infertility Treatment System";
            _enableSsl = _configuration.GetValue<bool>("SmtpSettings:EnableSsl");
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            if (!IsValidEmail(toEmail))
            {
                _logger.LogWarning("Invalid email address provided: {Email}", toEmail);
                throw new ArgumentException("Invalid email address", nameof(toEmail));
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                throw new ArgumentException("Subject cannot be empty", nameof(subject));
            }

            var fromAddress = new MailAddress(_fromEmail, _fromName);
            var toAddress = new MailAddress(toEmail);

            using var smtp = new SmtpClient
            {
                Host = _smtpHost,
                Port = _smtpPort,
                EnableSsl = _enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_smtpUser, _smtpPass)
            };

            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            })
            {
                // Add Reply-To header to show desired email address
                message.ReplyToList.Add(new MailAddress(_fromEmail, _fromName));
                
                // Add custom headers to try to override display address
                message.Headers.Add("X-Original-From", $"{_fromName} <{_fromEmail}>");
                message.Headers.Add("Return-Path", _fromEmail);
                
                // Try to set sender to desired address (some SMTP servers allow this)
                try
                {
                    message.Sender = new MailAddress(_fromEmail, _fromName);
                }
                catch
                {
                    // Some SMTP servers don't allow changing sender, ignore this error
                }

                try
                {
                    _logger.LogInformation("Attempting to send email to {ToEmail} using SMTP {Host}:{Port} with SSL: {EnableSsl}. From: {FromEmail}", 
                                          toEmail, _smtpHost, _smtpPort, _enableSsl, _fromEmail);
                    await smtp.SendMailAsync(message);
                    _logger.LogInformation("Email sent to {ToEmail} successfully. Subject: {Subject}", toEmail, subject);
                }
                catch (SmtpException ex)
                {
                    _logger.LogError(ex, "SMTP Error sending email to {ToEmail}. Status: {StatusCode}. Message: {Message}. SMTP Config - Host: {Host}, Port: {Port}, SSL: {EnableSsl}, User: {User}",
                                    toEmail, ex.StatusCode, ex.Message, _smtpHost, _smtpPort, _enableSsl, _smtpUser);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "General Error sending email to {ToEmail}. Message: {Message}. SMTP Config - Host: {Host}, Port: {Port}, SSL: {EnableSsl}, User: {User}",
                                     toEmail, ex.Message, _smtpHost, _smtpPort, _enableSsl, _smtpUser);
                    throw;
                }
            }
        }

        public async Task<EmailResponseDto> SendWelcomeEmailAsync(SendWelcomeEmailDto dto)
        {
            try
            {
                var htmlContent = EmailTemplates.WelcomeEmail(dto.CustomerName);
                await SendEmailAsync(dto.Email, "Welcome to Infertility Treatment System", htmlContent);
                
                return new EmailResponseDto
                {
                    Success = true,
                    Message = "Welcome email sent successfully",
                    SuccessCount = 1
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email to {Email}", dto.Email);
                return new EmailResponseDto
                {
                    Success = false,
                    Message = "Failed to send welcome email",
                    Errors = new List<string> { ex.Message },
                    FailureCount = 1
                };
            }
        }

        public async Task<EmailResponseDto> SendAppointmentConfirmationAsync(SendAppointmentConfirmationDto dto)
        {
            try
            {
                var appointment = await _unitOfWork.Appointments.GetByIdAsync(dto.AppointmentId);
                if (appointment == null)
                {
                    return new EmailResponseDto
                    {
                        Success = false,
                        Message = "Appointment not found",
                        Errors = new List<string> { "Appointment not found" },
                        FailureCount = 1
                    };
                }

                // Get customer and doctor information
                var cycle = appointment.CycleId == null ? null : await _unitOfWork.TreatmentCycles.GetCycleByIdAsync(appointment.CycleId.Value);
                var customer = await _unitOfWork.Customers.GetWithUserAsync(cycle.CustomerId);
                var doctor = await _unitOfWork.Doctors.GetDoctorByIdAsync(appointment.DoctorId);
                
                var appointmentDto = new Entity.DTOs.Appointments.AppointmentResponseDto
                {
                    Id = appointment.Id,
                    CycleId = appointment.CycleId,
                    DoctorId = appointment.DoctorId,
                    DoctorScheduleId = appointment.DoctorScheduleId,
                    AppointmentType = appointment.AppointmentType,
                    ScheduledDateTime = appointment.ScheduledDateTime,
                    Status = appointment.Status,
                    Notes = appointment.Notes,
                    Results = appointment.Results
                };

                var htmlContent = EmailTemplates.AppointmentConfirmation(
                    appointmentDto, 
                    customer.User?.FullName ?? "Valued Customer",
                    doctor.User?.FullName ?? "Doctor");

                await SendEmailAsync(dto.Email, "Appointment Confirmation", htmlContent);
                
                return new EmailResponseDto
                {
                    Success = true,
                    Message = "Appointment confirmation email sent successfully",
                    SuccessCount = 1
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send appointment confirmation email for appointment {AppointmentId}", dto.AppointmentId);
                return new EmailResponseDto
                {
                    Success = false,
                    Message = "Failed to send appointment confirmation email",
                    Errors = new List<string> { ex.Message },
                    FailureCount = 1
                };
            }
        }

        public async Task<EmailResponseDto> SendAppointmentReminderAsync(SendReminderDto dto)
        {
            try
            {
                var appointment = await _unitOfWork.Appointments.GetByIdAsync(dto.AppointmentId);
                if (appointment == null)
                {
                    return new EmailResponseDto
                    {
                        Success = false,
                        Message = "Appointment not found",
                        Errors = new List<string> { "Appointment not found" },
                        FailureCount = 1
                    };
                }

                // Get customer and doctor information
                var cycle = appointment.CycleId == null ? null : await _unitOfWork.TreatmentCycles.GetCycleByIdAsync(appointment.CycleId.Value);
                var customer = await _unitOfWork.Customers.GetWithUserAsync(cycle.CustomerId);
                var doctor = await _unitOfWork.Doctors.GetDoctorByIdAsync(appointment.DoctorId);
                
                var appointmentDto = new Entity.DTOs.Appointments.AppointmentResponseDto
                {
                    Id = appointment.Id,
                    CycleId = appointment.CycleId,
                    DoctorId = appointment.DoctorId,
                    DoctorScheduleId = appointment.DoctorScheduleId,
                    AppointmentType = appointment.AppointmentType,
                    ScheduledDateTime = appointment.ScheduledDateTime,
                    Status = appointment.Status,
                    Notes = appointment.Notes,
                    Results = appointment.Results
                };

                var htmlContent = EmailTemplates.AppointmentReminder(
                    appointmentDto, 
                    customer.User?.FullName ?? "Valued Customer",
                    doctor.User?.FullName ?? "Doctor");
                
                await SendEmailAsync(dto.Email, "Appointment Reminder", htmlContent);
                
                return new EmailResponseDto
                {
                    Success = true,
                    Message = "Appointment reminder email sent successfully",
                    SuccessCount = 1
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send appointment reminder email for appointment {AppointmentId}", dto.AppointmentId);
                return new EmailResponseDto
                {
                    Success = false,
                    Message = "Failed to send appointment reminder email",
                    Errors = new List<string> { ex.Message },
                    FailureCount = 1
                };
            }
        }

        public async Task<EmailResponseDto> SendTestResultNotificationAsync(SendTestResultDto dto)
        {
            try
            {
                var testResult = await _unitOfWork.TestResults.GetByIdAsync(dto.TestResultId);
                if (testResult == null)
                {
                    return new EmailResponseDto
                    {
                        Success = false,
                        Message = "Test result not found",
                        Errors = new List<string> { "Test result not found" },
                        FailureCount = 1
                    };
                }

                // Get customer information through cycle
                var cycle = await _unitOfWork.TreatmentCycles.GetCycleByIdAsync(testResult.CycleId);
                var customer = await _unitOfWork.Customers.GetWithUserAsync(cycle.CustomerId);
                
                var htmlContent = EmailTemplates.TestResultNotification(
                    testResult, 
                    customer.User?.FullName ?? "Valued Customer");
                
                await SendEmailAsync(dto.Email, "Test Results Available", htmlContent);
                
                return new EmailResponseDto
                {
                    Success = true,
                    Message = "Test result notification email sent successfully",
                    SuccessCount = 1
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send test result notification email for test result {TestResultId}", dto.TestResultId);
                return new EmailResponseDto
                {
                    Success = false,
                    Message = "Failed to send test result notification email",
                    Errors = new List<string> { ex.Message },
                    FailureCount = 1
                };
            }
        }

        public async Task<EmailResponseDto> SendTreatmentPhaseUpdateAsync(SendTreatmentPhaseUpdateDto dto)
        {
            try
            {
                var treatmentPhase = await _unitOfWork.TreatmentPhases.GetByIdAsync(dto.TreatmentPhaseId);
                if (treatmentPhase == null)
                {
                    return new EmailResponseDto
                    {
                        Success = false,
                        Message = "Treatment phase not found",
                        Errors = new List<string> { "Treatment phase not found" },
                        FailureCount = 1
                    };
                }

                // Get customer information through cycle
                var cycle = await _unitOfWork.TreatmentCycles.GetCycleByIdAsync(treatmentPhase.CycleId);
                var customer = await _unitOfWork.Customers.GetWithUserAsync(cycle.CustomerId);
                
                var htmlContent = EmailTemplates.TreatmentPhaseUpdate(
                    treatmentPhase, 
                    customer.User?.FullName ?? "Valued Customer");
                
                await SendEmailAsync(dto.Email, "Treatment Phase Update", htmlContent);
                
                return new EmailResponseDto
                {
                    Success = true,
                    Message = "Treatment phase update email sent successfully",
                    SuccessCount = 1
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send treatment phase update email for phase {TreatmentPhaseId}", dto.TreatmentPhaseId);
                return new EmailResponseDto
                {
                    Success = false,
                    Message = "Failed to send treatment phase update email",
                    Errors = new List<string> { ex.Message },
                    FailureCount = 1
                };
            }
        }

     

        public async Task<EmailResponseDto> SendBulkNotificationAsync(BulkEmailDto dto)
        {
            var result = new EmailResponseDto
            {
                Success = true,
                Message = "Bulk email operation completed"
            };

            var recipients = new List<string>(dto.Recipients);

            // If specific user roles are specified, get users by roles
            if (dto.UserRoles != null && dto.UserRoles.Any())
            {
                try
                {
                    var usersByRole = await _unitOfWork.Users.GetUsersByRolesAsync(dto.UserRoles);
                    foreach (var user in usersByRole)
                    {
                        if (!string.IsNullOrEmpty(user.Email) && !recipients.Contains(user.Email))
                        {
                            recipients.Add(user.Email);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get users by roles for bulk email");
                }
            }

            // If specific user IDs are specified, get those users
            if (dto.SpecificUserIds != null && dto.SpecificUserIds.Any())
            {
                try
                {
                    foreach (var userId in dto.SpecificUserIds)
                    {
                        var user = await _unitOfWork.Users.GetByIdAsync(userId);
                        if (user != null && !string.IsNullOrEmpty(user.Email) && !recipients.Contains(user.Email))
                        {
                            recipients.Add(user.Email);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get users by IDs for bulk email");
                }
            }

            foreach (var email in recipients)
            {
                try
                {
                    string htmlContent;
                    if (dto.IsHtml)
                    {
                        htmlContent = EmailTemplates.BulkNotificationTemplate(dto.Subject, dto.Message, "Valued Customer");
                    }
                    else
                    {
                        htmlContent = dto.Message;
                    }

                    await SendEmailAsync(email, dto.Subject, htmlContent);
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send bulk email to {Email}", email);
                    result.Errors.Add($"Failed to send to {email}: {ex.Message}");
                    result.FailureCount++;
                }
            }

            if (result.FailureCount > 0)
            {
                result.Success = false;
                result.Message = $"Bulk email completed with {result.FailureCount} failures out of {recipients.Count} total recipients";
            }
            else
            {
                result.Message = $"All {result.SuccessCount} emails sent successfully";
            }

            return result;
        }

        public async Task<bool> TestConfigurationAsync()
        {
            try
            {
                _logger.LogInformation("Testing email configuration - Host: {Host}, Port: {Port}, SSL: {EnableSsl}, User: {User}, FromEmail: {FromEmail}", 
                                      _smtpHost, _smtpPort, _enableSsl, _smtpUser, _fromEmail);

                using var smtp = new SmtpClient
                {
                    Host = _smtpHost,
                    Port = _smtpPort,
                    EnableSsl = _enableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_smtpUser, _smtpPass),
                    Timeout = 10000 // 10 second timeout
                };

                // Test connection by attempting to connect
                using var message = new MailMessage(_fromEmail, _fromEmail)
                {
                    Subject = "Configuration Test",
                    Body = "This is a configuration test email.",
                    IsBodyHtml = false
                };

                await smtp.SendMailAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email configuration test failed: {Message}", ex.Message);
                return false;
            }
        }
    }
}
