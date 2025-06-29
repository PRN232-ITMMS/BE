using InfertilityTreatment.Business.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using InfertilityTreatment.Entity.DTOs.Emails;
using System.Text.RegularExpressions;
using System.IO;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace InfertilityTreatment.Business.Services
{
    public class EmailTemplateConfig
    {
        public string Subject { get; set; } = string.Empty;
        public string HtmlFile { get; set; } = string.Empty;
    }

    public class EmailTemplatesConfig
    {
        public Dictionary<string, EmailTemplateConfig> EmailTemplates { get; set; } = new Dictionary<string, EmailTemplateConfig>();
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly bool _enableSsl;
        private readonly string _templatesPath;
        private readonly Dictionary<string, EmailTemplateConfig> _templateConfigs;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger, IHostEnvironment hostEnvironment)
        {
            _configuration = configuration;
            _logger = logger;
                _hostEnvironment = hostEnvironment;

                _smtpHost = _configuration["SmtpSettings:Host"] ?? throw new ArgumentNullException("SmtpSettings:Host not configured.");
                _smtpPort = _configuration.GetValue<int>("SmtpSettings:Port");
                _smtpUser = _configuration["SmtpSettings:Username"] ?? throw new ArgumentNullException("SmtpSettings:Username not configured.");
                _smtpPass = _configuration["SmtpSettings:Password"] ?? throw new ArgumentNullException("SmtpSettings:Password not configured.");
                _fromEmail = _configuration["SmtpSettings:FromEmail"] ?? throw new ArgumentNullException("SmtpSettings:FromEmail not configured.");
                _fromName = _configuration["SmtpSettings:FromName"] ?? "Infertility Treatment System";
                _enableSsl = _configuration.GetValue<bool>("SmtpSettings:EnableSsl");

                // Set templates path
                _templatesPath = Path.Combine(_hostEnvironment.ContentRootPath, "Templates", "Email");

                // Load template configurations
                _templateConfigs = LoadTemplateConfigurations();
            }

            public async Task SendEmailAsync(EmailDto emailDto)
            {
                var fromAddress = new MailAddress(_fromEmail, _fromName);
                var toAddress = new MailAddress(emailDto.ToEmail);

                using (var smtp = new SmtpClient
                {
                    Host = _smtpHost,
                    Port = _smtpPort,
                    EnableSsl = _enableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_smtpUser, _smtpPass)
                })
                {
                    using (var message = new MailMessage(fromAddress, toAddress)
                    {
                        Subject = emailDto.Subject,
                        Body = emailDto.Body,
                        IsBodyHtml = emailDto.IsHtml
                    })
                    {
                        if (emailDto.CcEmails != null)
                        {
                            foreach (var cc in emailDto.CcEmails) message.CC.Add(cc);
                        }
                        if (emailDto.BccEmails != null)
                        {
                            foreach (var bcc in emailDto.BccEmails) message.Bcc.Add(bcc);
                        }
                        if (emailDto.Attachments != null)
                        {
                            foreach (var attachmentPath in emailDto.Attachments)
                            {
                                try
                                {
                                    message.Attachments.Add(new Attachment(attachmentPath));
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Failed to attach file {FilePath} to email.", attachmentPath);
                                }
                            }
                        }

                        try
                        {
                            await smtp.SendMailAsync(message);
                            _logger.LogInformation("Email sent to {ToEmail} successfully. Subject: {Subject}", emailDto.ToEmail, emailDto.Subject);
                        }
                        catch (SmtpException ex)
                        {
                            _logger.LogError(ex, "SMTP Error sending email to {ToEmail}. Status: {StatusCode}. Message: {Message}",
                                            emailDto.ToEmail, ex.StatusCode, ex.Message);
                            throw;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "General Error sending email to {ToEmail}. Message: {Message}",
                                             emailDto.ToEmail, ex.Message);
                            throw;
                        }
                    }
                }
            }

            public async Task SendTemplateEmailAsync(string templateName, Dictionary<string, string> data, string toEmail, string subject)
            {
                try
                {
                    // Get template configuration
                    if (!_templateConfigs.ContainsKey(templateName))
                    {
                        _logger.LogError("Email template '{TemplateName}' not found in configuration.", templateName);
                        throw new ArgumentException($"Email template '{templateName}' not found.", nameof(templateName));
                    }

                    var templateConfig = _templateConfigs[templateName];
                    var templateSubject = templateConfig.Subject ?? subject;
                    var htmlFileName = templateConfig.HtmlFile;

                    // Load HTML template
                    var templatePath = Path.Combine(_templatesPath, htmlFileName);
                    if (!File.Exists(templatePath))
                    {
                        _logger.LogError("Email template file '{TemplatePath}' not found.", templatePath);
                        throw new FileNotFoundException($"Email template file '{templatePath}' not found.");
                    }

                    var templateBody = await File.ReadAllTextAsync(templatePath);

                    if (string.IsNullOrEmpty(templateBody))
                    {
                        _logger.LogError("Email template '{TemplateName}' body is empty.", templateName);
                        throw new ArgumentException($"Email template '{templateName}' body is empty.", nameof(templateName));
                    }

                    var renderedSubject = ReplacePlaceholders(templateSubject, data);
                    var renderedBody = ReplacePlaceholders(templateBody, data);

                    var emailDto = new EmailDto
                    {
                        ToEmail = toEmail,
                        Subject = renderedSubject,
                        Body = renderedBody,
                        IsHtml = true
                    };

                    await SendEmailAsync(emailDto);
                    _logger.LogInformation("Template email '{TemplateName}' sent successfully to {ToEmail}", templateName, toEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending template email '{TemplateName}' to {ToEmail}", templateName, toEmail);
                    throw;
                }
            }

            private Dictionary<string, EmailTemplateConfig> LoadTemplateConfigurations()
            {
                try
                {
                    var templateConfigPath = Path.Combine(_templatesPath, "templates.json");
                    if (!File.Exists(templateConfigPath))
                    {
                        _logger.LogWarning("Template configuration file not found at {ConfigPath}. Using default configuration.", templateConfigPath);
                        return GetDefaultTemplateConfigurations();
                    }

                    var configJson = File.ReadAllText(templateConfigPath);
                    var config = JsonConvert.DeserializeObject<EmailTemplatesConfig>(configJson);

                    if (config?.EmailTemplates == null)
                    {
                        _logger.LogWarning("Invalid template configuration format. Using default configuration.");
                        return GetDefaultTemplateConfigurations();
                    }

                    _logger.LogInformation("Loaded {Count} email template configurations", config.EmailTemplates.Count);
                    return config.EmailTemplates;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading template configurations. Using default configuration.");
                    return GetDefaultTemplateConfigurations();
                }
            }

            private Dictionary<string, EmailTemplateConfig> GetDefaultTemplateConfigurations()
            {
                return new Dictionary<string, EmailTemplateConfig>
                {
                    ["AppointmentReminder"] = new EmailTemplateConfig { Subject = "Appointment Reminder - {{AppointmentType}} with {{DoctorName}}", HtmlFile = "AppointmentReminder.html" },
                    ["TestResultsAvailable"] = new EmailTemplateConfig { Subject = "Your {{TestType}} Test Results Are Available", HtmlFile = "TestResultsAvailable.html" },
                    ["CriticalTestResultAlert"] = new EmailTemplateConfig { Subject = "URGENT: Critical Test Result - Immediate Attention Required", HtmlFile = "CriticalTestResultAlert.html" },
                    ["TreatmentCycleUpdate"] = new EmailTemplateConfig { Subject = "Treatment Cycle Status Update - Cycle {{CycleId}}", HtmlFile = "TreatmentCycleUpdate.html" },
                    ["GeneralNotification"] = new EmailTemplateConfig { Subject = "Important Notification from Infertility Treatment Clinic", HtmlFile = "GeneralNotification.html" }
                };
            }

            private string ReplacePlaceholders(string template, Dictionary<string, string> data)
            {
                var result = template;
                if (data == null) return result;
                foreach (var entry in data)
                {
                    result = Regex.Replace(result, Regex.Escape("{{" + entry.Key + "}}"), entry.Value, RegexOptions.IgnoreCase);
                }
                return result;
            }
        }
    }
