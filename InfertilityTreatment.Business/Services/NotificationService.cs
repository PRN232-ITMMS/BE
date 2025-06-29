using InfertilityTreatment.Business.Interfaces;
using InfertilityTreatment.Data.Repositories.Interfaces;
using InfertilityTreatment.Entity.DTOs.Emails;
using InfertilityTreatment.Entity.DTOs.Notifications;
using InfertilityTreatment.Entity.Entities;
using InfertilityTreatment.Entity.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Business.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IEmailQueueService _emailQueueService;
        private readonly IBaseRepository<User> _userRepository;
        private readonly IUserEmailPreferenceService _userEmailPreferenceService;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepository notificationRepository,
            IEmailQueueService emailQueueService,
            IBaseRepository<User> userRepository,
            IUserEmailPreferenceService userEmailPreferenceService,
            ILogger<NotificationService> logger)
        {
            _notificationRepository = notificationRepository;
            _emailQueueService = emailQueueService;
            _userRepository = userRepository;
            _userEmailPreferenceService = userEmailPreferenceService;
            _logger = logger;
        }
        public async Task<bool> CreateNotificationAsync(CreateNotificationDto createDto)
        {
            var user = await _userRepository.GetByIdAsync(createDto.UserId);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found for notification creation. Notification not created.", createDto.UserId);
                return false;
            }

            var notification = new Notification
            {
                UserId = createDto.UserId,
                Title = createDto.Title,
                Message = createDto.Message,
                Type = createDto.Type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                RelatedEntityId = createDto.RelatedEntityId,
                RelatedEntityType = createDto.RelatedEntityType,
                ScheduledAt = createDto.ScheduledAt,
                SentAt = null,
                EmailStatus = createDto.SendEmail ? EmailStatus.Pending : EmailStatus.DisabledByUser
            };

            await _notificationRepository.AddAsync(notification);
            var saved = await _notificationRepository.SaveChangesAsync();

            if (saved > 0 && createDto.SendEmail)
            {
                var userEmailPreference = await _userEmailPreferenceService.GetUserPreferenceAsync(createDto.UserId, createDto.Type);

                if (userEmailPreference != null && !userEmailPreference.IsEnabled)
                {
                    _logger.LogInformation("Email notification for user {UserId}, type {NotificationType} is disabled by user preferences. Email not queued.", user.Id, createDto.Type);
                    notification.EmailStatus = EmailStatus.DisabledByUser;
                    await _notificationRepository.UpdateAsync(notification);
                    await _notificationRepository.SaveChangesAsync();
                    return true;
                }

                if (createDto.ScheduledAt.HasValue && createDto.ScheduledAt.Value > DateTime.UtcNow)
                {
                    _logger.LogInformation("Notification ID {NotificationId} (Type: {Type}) scheduled for future sending at {ScheduledAt}. EmailStatus set to Pending.", notification.Id, notification.Type, notification.ScheduledAt);
                    return true;
                }

                // Nếu không có ScheduledAt hoặc đã đến thời gian gửi, đưa vào queue ngay lập tức
                await EnqueueEmailForNotification(notification, user.Email, createDto.EmailSubject, createDto.EmailTemplateName, createDto.EmailTemplateData);
            }
            return saved > 0;
        }
        public async Task<List<NotificationResponseDto>> GetUserNotificationsAsync(
            int userId,
            DateTime? sentAtStart = null,
            DateTime? sentAtEnd = null,
            bool? isRead = null,
            string sortBy = "CreatedAtDesc")
        {
            var notifications = await _notificationRepository.GetNotificationsByUserIdAsync(userId, sentAtStart, sentAtEnd, isRead, sortBy);

            return notifications.Select(n => new NotificationResponseDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                RelatedEntityId = n.RelatedEntityId,
                RelatedEntityType = n.RelatedEntityType,
                ScheduledAt = n.ScheduledAt,
                SentAt = n.SentAt,
                EmailStatus = n.EmailStatus
            }).ToList();
        }
        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification == null)
            {
                _logger.LogWarning("Notification with ID {NotificationId} not found for marking as read.", notificationId);
                return false;
            }

            notification.IsRead = true;
            await _notificationRepository.UpdateAsync(notification);
            return await _notificationRepository.SaveChangesAsync() > 0;
        }
        public async Task<bool> DeleteNotificationAsync(int notificationId)
        {
            var exists = await _notificationRepository.ExistsAsync(notificationId);
            if (!exists)
            {
                _logger.LogWarning("Notification with ID {NotificationId} not found for deletion.", notificationId);
                return false;
            }

            await _notificationRepository.DeleteAsync(notificationId);
            return await _notificationRepository.SaveChangesAsync() > 0;
        }
        public async Task<Notification?> GetNotificationIfOwnedAsync(int notificationId, int userId)
        {
            return await _notificationRepository.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
        }
        public async Task MarkNotificationEmailStatusAsync(int notificationId, EmailStatus status)
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification != null)
            {
                notification.EmailStatus = status;
                if (status == EmailStatus.Sent)
                {
                    notification.SentAt = DateTime.UtcNow;
                }
                await _notificationRepository.UpdateAsync(notification);
                await _notificationRepository.SaveChangesAsync();
                _logger.LogInformation("Notification {NotificationId} email status updated to {Status}. SentAt: {SentAt}", notificationId, status, notification.SentAt);
            }
            else
            {
                _logger.LogWarning("Notification with ID {NotificationId} not found for email status update.", notificationId);
            }
        }
        public async Task ProcessScheduledEmailNotification(
            int notificationId,
            string subject,
            string message,
            string? templateName,
            Dictionary<string, string>? templateData)
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification == null)
            {
                _logger.LogWarning("Scheduled Notification ID {NotificationId} not found for processing email.", notificationId);
                return;
            }

            var user = await _userRepository.GetByIdAsync(notification.UserId);
            if (user == null || string.IsNullOrEmpty(user.Email))
            {
                _logger.LogWarning("User for Scheduled Notification ID {NotificationId} not found or has no email. Marking as Failed.", notificationId);
                await MarkNotificationEmailStatusAsync(notificationId, EmailStatus.Failed);
                return;
            }

            await EnqueueEmailForNotification(notification, user.Email, subject, templateName, templateData);
        }

        public async Task<List<Notification>> GetScheduledPendingNotificationsAsync()
        {
            return await _notificationRepository.GetScheduledPendingNotificationsAsync();
        }
        private async Task EnqueueEmailForNotification(
            Notification notification,
            string toEmail,
            string? subject,
            string? templateName,
            Dictionary<string, string>? templateData)
        {
            var emailDto = new EmailDto
            {
                ToEmail = toEmail,
                Subject = subject ?? notification.Title,
                Body = notification.Message,
                IsHtml = true
            };

            var queuedEmailItem = new QueuedEmailItemDto
            {
                EmailDto = emailDto,
                TemplateName = templateName,
                TemplateData = templateData,
                NotificationId = notification.Id
            };

            _emailQueueService.EnqueueEmail(queuedEmailItem);
            notification.EmailStatus = EmailStatus.Queued;
            await _notificationRepository.UpdateAsync(notification);
            await _notificationRepository.SaveChangesAsync();
            _logger.LogInformation("Email for Notification ID {NotificationId} (Type: {Type}) added to queue for {ToEmail}.", notification.Id, notification.Type, toEmail);
        }
    }
}
