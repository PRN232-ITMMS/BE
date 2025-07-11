using InfertilityTreatment.Business.Interfaces;
using InfertilityTreatment.Entity.DTOs.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InfertilityTreatment.API.Controllers
{
    [ApiController]
    [Route("api/emails/")]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailController> _logger;

        public EmailController(IEmailService emailService, ILogger<EmailController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Send welcome email to new customers
        /// </summary>
        [HttpPost("send-welcome")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> SendWelcomeEmail([FromBody] SendWelcomeEmailDto dto)
        {
            try
            {
                var result = await _emailService.SendWelcomeEmailAsync(dto);
                
                if (result.Success)
                {
                    return Ok(new { success = true, message = result.Message });
                }
                
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email to {Email}", dto.Email);
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Send appointment confirmation email
        /// </summary>
        [HttpPost("send-appointment-confirmation")]
        [Authorize(Roles = "Doctor,Admin,Staff")]
        public async Task<IActionResult> SendAppointmentConfirmation([FromBody] SendAppointmentConfirmationDto dto)
        {
            try
            {
                var result = await _emailService.SendAppointmentConfirmationAsync(dto);
                
                if (result.Success)
                {
                    return Ok(new { success = true, message = result.Message });
                }
                
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send appointment confirmation email for appointment {AppointmentId}", dto.AppointmentId);
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Send appointment reminder email
        /// </summary>
        [HttpPost("send-appointment-reminder")]
        [Authorize(Roles = "Doctor,Admin,Staff")]
        public async Task<IActionResult> SendAppointmentReminder([FromBody] SendReminderDto dto)
        {
            try
            {
                var result = await _emailService.SendAppointmentReminderAsync(dto);
                
                if (result.Success)
                {
                    return Ok(new { success = true, message = result.Message });
                }
                
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send appointment reminder email for appointment {AppointmentId}", dto.AppointmentId);
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Send test result notification email
        /// </summary>
        [HttpPost("send-test-result")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> SendTestResultEmail([FromBody] SendTestResultDto dto)
        {
            try
            {
                var result = await _emailService.SendTestResultNotificationAsync(dto);
                
                if (result.Success)
                {
                    return Ok(new { success = true, message = result.Message });
                }
                
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send test result email for test result {TestResultId}", dto.TestResultId);
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Send treatment phase update email
        /// </summary>
        [HttpPost("send-treatment-phase-update")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> SendTreatmentPhaseUpdate([FromBody] SendTreatmentPhaseUpdateDto dto)
        {
            try
            {
                var result = await _emailService.SendTreatmentPhaseUpdateAsync(dto);
                
                if (result.Success)
                {
                    return Ok(new { success = true, message = result.Message });
                }
                
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send treatment phase update email for phase {TreatmentPhaseId}", dto.TreatmentPhaseId);
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

       

        /// <summary>
        /// Send bulk notification emails
        /// </summary>
        [HttpPost("send-bulk-notification")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SendBulkNotification([FromBody] BulkEmailDto dto)
        {
            try
            {
                var result = await _emailService.SendBulkNotificationAsync(dto);
                
                return Ok(new { 
                    success = result.Success, 
                    message = result.Message,
                    successCount = result.SuccessCount,
                    failureCount = result.FailureCount,
                    errors = result.Errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bulk notification emails");
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Test email configuration by sending a test email
        /// </summary>
        [HttpPost("test-configuration")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TestEmailConfiguration([FromBody] TestEmailDto dto)
        {
            try
            {
                // First, test the configuration
                var configTest = await _emailService.TestConfigurationAsync();
                if (!configTest)
                {
                    return StatusCode(500, new { 
                        success = false, 
                        message = "Email configuration test failed", 
                        error = "SMTP configuration is invalid. Please check your email settings in appsettings.json" 
                    });
                }

                // If configuration is valid, send test email
                await _emailService.SendEmailAsync(dto.Email, "Test Email", 
                    "<h1>Test Email</h1><p>This is a test email to verify email configuration is working correctly.</p><p>Sent at: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "</p>");
                
                return Ok(new { success = true, message = "Test email sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send test email to {Email}", dto.Email);
                
                // Provide more specific error messages based on the exception type
                string errorMessage = ex.Message;
                if (ex.Message.Contains("Authentication Required") || ex.Message.Contains("5.7.0"))
                {
                    errorMessage = "SMTP Authentication failed. Please verify your email username and password. For Gmail, ensure you're using an App Password instead of your regular password.";
                }
                else if (ex.Message.Contains("connection") || ex.Message.Contains("timeout"))
                {
                    errorMessage = "Unable to connect to SMTP server. Please check your host and port settings.";
                }
                
                return StatusCode(500, new { 
                    success = false, 
                    message = "Email configuration test failed", 
                    error = errorMessage,
                    technicalDetails = ex.Message
                });
            }
        }
    }


}
