using InfertilityTreatment.Entity.DTOs.Notifications;
using InfertilityTreatment.Entity.Entities;
using InfertilityTreatment.Entity.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Business.Interfaces
{
    public interface INotificationService
    {
        Task<bool> CreateNotificationAsync(CreateNotificationDto createDto);
        Task<List<NotificationResponseDto>> GetUserNotificationsAsync(
            int userId,
            DateTime? sentAtStart = null,
            DateTime? sentAtEnd = null,
            bool? isRead = null,
            string sortBy = "CreatedAtDesc");
        Task<bool> MarkAsReadAsync(int notificationId);
        Task<bool> DeleteNotificationAsync(int notificationId);
        Task<Notification?> GetNotificationIfOwnedAsync(int notificationId, int userId);
        Task MarkNotificationEmailStatusAsync(int notificationId, EmailStatus status);
        Task ProcessScheduledEmailNotification(
            int notificationId,
            string subject,
            string message,
            string? templateName,
            Dictionary<string, string>? templateData);
        Task<List<Notification>> GetScheduledPendingNotificationsAsync();
    }
}
