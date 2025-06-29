using InfertilityTreatment.Business.Interfaces;
using InfertilityTreatment.Data.Repositories.Interfaces;
using InfertilityTreatment.Entity.Entities;
using InfertilityTreatment.Entity.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Business.Services
{
    public class ScheduledNotificationProcessorService : BackgroundService
    {
        private readonly ILogger<ScheduledNotificationProcessorService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private TimeSpan _pollingInterval = TimeSpan.FromMinutes(1);

        public ScheduledNotificationProcessorService(
            ILogger<ScheduledNotificationProcessorService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Scheduled Notification Processor Service đang khởi động.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                        var appointmentService = scope.ServiceProvider.GetRequiredService<IAppointmentService>();
                        var testResultService = scope.ServiceProvider.GetRequiredService<ITestResultService>();
                        var cycleService = scope.ServiceProvider.GetRequiredService<ICycleService>();
                        var scheduledNotifications = await notificationService.GetScheduledPendingNotificationsAsync();

                        if (scheduledNotifications.Any())
                        {
                            _logger.LogInformation("Tìm thấy {Count} thông báo hẹn giờ cần xử lý.", scheduledNotifications.Count);
                            foreach (var notification in scheduledNotifications)
                            {
                                await ProcessScheduledNotification(notification, notificationService, appointmentService, testResultService, cycleService);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Scheduled Notification Processor Service đang dừng.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Đã xảy ra lỗi trong Scheduled Notification Processor Service.");
                }

                await Task.Delay(_pollingInterval, stoppingToken);
            }
        }

        private async Task ProcessScheduledNotification(
            Notification notification,
            INotificationService notificationService,
            IAppointmentService appointmentService,
            ITestResultService testResultService,
            ICycleService cycleService)
        {
            _logger.LogInformation("Xử lý thông báo hẹn giờ ID: {NotificationId}, Title: '{Title}', ScheduledAt: {ScheduledAt}",
                                   notification.Id, notification.Title, notification.ScheduledAt);

            string emailSubject = notification.Title;
            string emailMessage = notification.Message;
            string? templateName = null;
            Dictionary<string, string> templateData = new Dictionary<string, string>();

            try
            {
                switch (notification.Type)
                {
                    case NotificationType.Reminder:
                        if (notification.RelatedEntityType == "Appointment" && notification.RelatedEntityId.HasValue)
                        {
                            var appointment = await appointmentService.GetByIdAsync(notification.RelatedEntityId.Value);
                            if (appointment != null)
                            {
                                templateName = "AppointmentReminder";
                                var customerUser = (await notificationService.GetNotificationIfOwnedAsync(notification.Id, notification.UserId))?.User;
                                var doctorUser = (await appointmentService.GetByIdAsync(appointment.Id))?.DoctorId;
                                var doctorEntity = (doctorUser.HasValue) ? (await _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IBaseRepository<Doctor>>().GetByIdAsync(doctorUser.Value)) : null; // Lấy Doctor entity
                                var doctorFullName = (doctorEntity != null) ? (await _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IBaseRepository<User>>().GetByIdAsync(doctorEntity.UserId))?.FullName : "Bác sĩ";


                                templateData.Add("CustomerName", customerUser?.FullName ?? "Khách hàng");
                                templateData.Add("DoctorName", doctorFullName ?? "Bác sĩ");
                                templateData.Add("AppointmentDateTime", appointment.ScheduledDateTime.ToString("dd/MM/yyyy HH:mm"));
                                templateData.Add("AppointmentType", appointment.AppointmentType.ToString());
                                emailSubject = "Nhắc nhở lịch hẹn sắp tới - Infertility Treatment";
                                emailMessage = $"Bạn có lịch hẹn vào lúc {appointment.ScheduledDateTime:dd/MM/yyyy HH:mm} ngày mai.";
                            }
                        }
                        break;
                    case NotificationType.CriticalTestResult:
                        if (notification.RelatedEntityType == "TestResult" && notification.RelatedEntityId.HasValue)
                        {
                            var testResult = await testResultService.GetTestResultByIdAsync(notification.RelatedEntityId.Value);
                            if (testResult != null)
                            {
                                templateName = "CriticalTestResultAlert";
                                var customerUser = (await notificationService.GetNotificationIfOwnedAsync(notification.Id, notification.UserId))?.User;
                                var cycleDetail = await cycleService.GetCycleByIdAsync(testResult.CycleId);
                                var doctorEntity = (cycleDetail != null) ? (await _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IBaseRepository<Doctor>>().GetByIdAsync(cycleDetail.DoctorId)) : null;
                                var doctorUser = (doctorEntity != null) ? (await _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IBaseRepository<User>>().GetByIdAsync(doctorEntity.UserId)) : null;

                                templateData.Add("DoctorName", doctorUser?.FullName ?? "Bác sĩ");
                                templateData.Add("CustomerName", customerUser?.FullName ?? "Bệnh nhân");
                                templateData.Add("CycleId", testResult.CycleId.ToString());
                                templateData.Add("TestType", testResult.TestType.ToString());
                                templateData.Add("TestResults", testResult.Results);
                                templateData.Add("ReferenceRange", testResult.ReferenceRange ?? "N/A");
                                emailSubject = "[KHẨN CẤP] Kết quả xét nghiệm quan trọng - Infertility Treatment";
                                emailMessage = $"Một kết quả xét nghiệm quan trọng của bệnh nhân {templateData["CustomerName"]} đã được ghi nhận. Loại xét nghiệm: {testResult.TestType}.";
                            }
                        }
                        break;
                    case NotificationType.TreatmentCycleStatusChange:
                        if (notification.RelatedEntityType == "TreatmentCycle" && notification.RelatedEntityId.HasValue)
                        {
                            var cycleDetail = await cycleService.GetCycleByIdAsync(notification.RelatedEntityId.Value);
                            if (cycleDetail != null)
                            {
                                templateName = "TreatmentCycleStatusChange";
                                var customerUser = (await notificationService.GetNotificationIfOwnedAsync(notification.Id, notification.UserId))?.User;
                                templateData.Add("CustomerName", customerUser?.FullName ?? "Khách hàng");
                                templateData.Add("CycleId", cycleDetail.Id.ToString());
                                templateData.Add("OldStatus", cycleDetail.Status.ToString()); 
                                templateData.Add("NewStatus", cycleDetail.Status.ToString());
                                emailSubject = "Cập nhật trạng thái chu kỳ điều trị của bạn - Infertility Treatment";
                                emailMessage = $"Trạng thái chu kỳ điều trị của bạn (Cycle ID: {cycleDetail.Id}) đã thay đổi.";
                            }
                        }
                        break;
                    default:
                        _logger.LogWarning("Không tìm thấy template cụ thể cho Notification ID {NotificationId}, Type {Type}. Sẽ sử dụng tiêu đề và nội dung mặc định.", notification.Id, notification.Type);
                        break;
                }

                await notificationService.ProcessScheduledEmailNotification(
                    notification.Id,
                    emailSubject,
                    emailMessage,
                    templateName,
                    templateData
                );

                _logger.LogInformation("Thông báo hẹn giờ ID {NotificationId} đã được đưa vào hàng đợi gửi email.", notification.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý thông báo hẹn giờ ID {NotificationId} để gửi email. EmailStatus sẽ vẫn là Pending.", notification.Id);
                await notificationService.MarkNotificationEmailStatusAsync(notification.Id, EmailStatus.Failed);
            }
        }
    }
}
