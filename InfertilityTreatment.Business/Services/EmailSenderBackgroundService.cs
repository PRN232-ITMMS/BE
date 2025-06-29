using InfertilityTreatment.Business.Interfaces;
using InfertilityTreatment.Entity.DTOs.Emails;
using InfertilityTreatment.Entity.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Business.Services
{
    public class EmailSenderBackgroundService : BackgroundService
    {
        private readonly IEmailQueueService _emailQueueService;
        private readonly ILogger<EmailSenderBackgroundService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        private const int MaxRetries = 3;
        private const int RetryDelayMs = 5000;

        public EmailSenderBackgroundService(
            IEmailQueueService emailQueueService,
            ILogger<EmailSenderBackgroundService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _emailQueueService = emailQueueService;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Email Sender Background Service đang khởi động.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var emailItem = _emailQueueService.DequeueEmail();
                    if (emailItem != null)
                    {
                        await ProcessEmailItem(emailItem);
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Email Sender Background Service đang dừng.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Đã xảy ra lỗi trong khi xử lý hàng đợi email.");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }
        }

        private async Task ProcessEmailItem(QueuedEmailItemDto emailItem)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                try
                {
                    if (!string.IsNullOrEmpty(emailItem.TemplateName))
                    {
                        await emailService.SendTemplateEmailAsync(
                            emailItem.TemplateName,
                            emailItem.TemplateData ?? new Dictionary<string, string>(),
                            emailItem.EmailDto.ToEmail,
                            emailItem.EmailDto.Subject
                        );
                    }
                    else
                    {
                        await emailService.SendEmailAsync(emailItem.EmailDto);
                    }

                    await notificationService.MarkNotificationEmailStatusAsync(emailItem.NotificationId, EmailStatus.Sent);
                    _logger.LogInformation("Email cho NotificationId {NotificationId} đã được gửi thành công đến {ToEmail}.", emailItem.NotificationId, emailItem.EmailDto.ToEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Không thể gửi email cho NotificationId {NotificationId}. Đang thử lại ({RetryCount}/{MaxRetries}).", emailItem.NotificationId, emailItem.RetryCount + 1, MaxRetries);

                    emailItem.RetryCount++;
                    if (emailItem.RetryCount < MaxRetries)
                    {
                        _emailQueueService.EnqueueEmail(emailItem);
                        await notificationService.MarkNotificationEmailStatusAsync(emailItem.NotificationId, EmailStatus.Failed); // Hoặc EmailStatus.PendingRetry
                        await Task.Delay(RetryDelayMs);
                    }
                    else
                    {
                        await notificationService.MarkNotificationEmailStatusAsync(emailItem.NotificationId, EmailStatus.Failed);
                        _logger.LogError("Email cho NotificationId {NotificationId} đã thất bại sau {MaxRetries} lần thử lại. Sẽ không thử lại nữa.", emailItem.NotificationId, MaxRetries);
                    }
                }
            }
        }
    }
}
