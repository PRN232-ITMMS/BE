using AutoMapper;
using InfertilityTreatment.Business.Interfaces;
using InfertilityTreatment.Data.Repositories.Implementations;
using InfertilityTreatment.Data.Repositories.Interfaces;
using InfertilityTreatment.Entity.Constants;
using InfertilityTreatment.Entity.DTOs.Appointments;
using InfertilityTreatment.Entity.DTOs.Common;
using InfertilityTreatment.Entity.DTOs.DoctorSchedules;
using InfertilityTreatment.Entity.DTOs.Notifications;
using InfertilityTreatment.Entity.DTOs.TreatmentPakages;
using InfertilityTreatment.Entity.Entities;
using InfertilityTreatment.Entity.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Business.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;
        private readonly IBaseRepository<User> _userRepository;
        private readonly ILogger<AppointmentService> _logger;

        public AppointmentService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            INotificationService notificationService,
            IBaseRepository<User> userRepository,
            ILogger<AppointmentService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _notificationService = notificationService;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<AppointmentResponseDto> CreateAppointmentAsync(CreateAppointmentDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Check existence of related entities
                var cycle = await _unitOfWork.TreatmentCycles.GetCycleByIdAsync(dto.CycleId);
                if (cycle == null)
                    throw new ArgumentException("Cycle not found");
                var doctor = await _unitOfWork.Doctors.GetDoctorByIdAsync(dto.DoctorId);
                if (doctor == null)
                    throw new ArgumentException("Doctor not found");
                var doctorSchedule = await _unitOfWork.DoctorSchedules.GetByIdAsync(dto.DoctorScheduleId);
                if (doctorSchedule == null)
                    throw new ArgumentException("DoctorSchedule not found");

                // Check for conflict: same doctor, same date, same slot
                var conflict = await _unitOfWork.Appointments.GetByDoctorAndScheduleAsync(dto.DoctorId, dto.ScheduledDateTime, dto.DoctorScheduleId);
                if (conflict != null)
                {
                    throw new InvalidOperationException("Doctor already has an appointment at this time slot.");
                }

                var appointment = new Appointment
                {
                    CycleId = dto.CycleId,
                    DoctorId = dto.DoctorId,
                    DoctorScheduleId = dto.DoctorScheduleId,
                    AppointmentType = dto.AppointmentType,
                    ScheduledDateTime = dto.ScheduledDateTime,
                    Notes = dto.Notes,
                    Status = AppointmentStatus.Scheduled
                };

                var created = await _unitOfWork.Appointments.CreateAsync(appointment);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                // --- TRIGGER LOGIC FOR NOTIFICATIONS AND EMAILS ---
                try
                {
                    var customer = await _unitOfWork.Customers.GetByIdAsync(cycle.CustomerId);
                    var customerUser = (customer != null) ? await _userRepository.GetByIdAsync(customer.UserId) : null;

                    var doctorUser = await _userRepository.GetByIdAsync(doctor.UserId);

                    if (customerUser != null)
                    {
                        var customerData = new Dictionary<string, string>
                        {
                            { "CustomerName", customerUser.FullName },
                            { "DoctorName", doctorUser?.FullName ?? "Bác sĩ" },
                            { "AppointmentDateTime", appointment.ScheduledDateTime.ToString("dd/MM/yyyy HH:mm") },
                            { "AppointmentType", appointment.AppointmentType.ToString() }
                        };
                        await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                        {
                            UserId = customerUser.Id,
                            Title = "Xác nhận lịch hẹn của bạn",
                            Message = $"Lịch hẹn của bạn với {doctorUser?.FullName ?? "bác sĩ"} vào lúc {appointment.ScheduledDateTime:dd/MM/yyyy HH:mm} đã được xác nhận.",
                            Type = NotificationType.Appointment,
                            RelatedEntityType = "Appointment",
                            RelatedEntityId = created.Id,
                            SendEmail = true,
                            EmailTemplateName = "AppointmentConfirmation",
                            EmailTemplateData = customerData
                        });
                        _logger.LogInformation("Gửi xác nhận lịch hẹn cho khách hàng {CustomerId}.", customerUser.Id);
                    }

                    // 2. Appointment Reminder Notification (scheduled 24h before)
                    var reminderTime = appointment.ScheduledDateTime.AddHours(-24);
                    if (reminderTime > DateTime.UtcNow)
                    {
                        if (customerUser != null)
                        {
                            var customerData = new Dictionary<string, string>
                            {
                                { "CustomerName", customerUser.FullName },
                                { "DoctorName", doctorUser?.FullName ?? "Bác sĩ" },
                                { "AppointmentDateTime", appointment.ScheduledDateTime.ToString("dd/MM/yyyy HH:mm") },
                                { "AppointmentType", appointment.AppointmentType.ToString() }
                            };
                            await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                            {
                                UserId = customerUser.Id,
                                Title = "Nhắc nhở lịch hẹn sắp tới",
                                Message = $"Bạn có lịch hẹn với {doctorUser?.FullName ?? "bác sĩ"} vào lúc {appointment.ScheduledDateTime:dd/MM/yyyy HH:mm} ngày mai.",
                                Type = NotificationType.Reminder,
                                RelatedEntityType = "Appointment",
                                RelatedEntityId = created.Id,
                                ScheduledAt = reminderTime,
                                SendEmail = true,
                                EmailTemplateName = "AppointmentReminder",
                                EmailTemplateData = customerData
                            });
                            _logger.LogInformation("Lên lịch nhắc nhở lịch hẹn cho khách hàng {CustomerId} vào {ReminderTime}.", customerUser.Id, reminderTime);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi tạo thông báo cho lịch hẹn mới {AppointmentId}.", created.Id);
                }

                return new AppointmentResponseDto
                {
                    Id = created.Id,
                    DoctorId = created.DoctorId,
                    DoctorScheduleId = created.DoctorScheduleId,
                    AppointmentType = created.AppointmentType,
                    ScheduledDateTime = created.ScheduledDateTime,
                    Status = created.Status,
                    Notes = created.Notes,
                    Results = created.Results
                };
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<PaginatedResultDto<AppointmentResponseDto>> GetAppointmentsByCustomerAsync(int customerId, PaginationQueryDTO pagination)
        {
            var list = await _unitOfWork.Appointments.GetByCustomerAsync(customerId, pagination);
            var dtoList = list.Items.Select(a => new AppointmentResponseDto
            {
                Id = a.Id,
                DoctorId = a.DoctorId,
                DoctorScheduleId = a.DoctorScheduleId,
                AppointmentType = a.AppointmentType,
                ScheduledDateTime = a.ScheduledDateTime,
                Status = a.Status,
                Notes = a.Notes,
                Results = a.Results
            }).ToList();

            return new PaginatedResultDto<AppointmentResponseDto>(
                dtoList,
                list.TotalCount,
                pagination.PageNumber,
                pagination.PageSize
            );
        }

        public async Task<PaginatedResultDto<AppointmentResponseDto>> GetAppointmentsByDoctorAsync(int doctorId, DateTime date, PaginationQueryDTO pagination)
        {
            var list = await _unitOfWork.Appointments.GetByDoctorAndDateAsync(doctorId, date, pagination);
            var dtoList = list.Items.Select(a => new AppointmentResponseDto
            {
                Id = a.Id,
                DoctorId = a.DoctorId,
                DoctorScheduleId = a.DoctorScheduleId,
                AppointmentType = a.AppointmentType,
                ScheduledDateTime = a.ScheduledDateTime,
                Status = a.Status,
                Notes = a.Notes,
                Results = a.Results
            }).ToList();

            return new PaginatedResultDto<AppointmentResponseDto>(
                dtoList,
                list.TotalCount,
                pagination.PageNumber,
                pagination.PageSize
            );
        }

        public async Task<AppointmentResponseDto> RescheduleAppointmentAsync(int id, int doctorScheduleId, DateTime scheduledDateTime)
        {
            if (id <= 0)
                throw new ArgumentException("AppointmentId is required and must be greater than 0");
            if (doctorScheduleId <= 0)
                throw new ArgumentException("DoctorScheduleId is required and must be greater than 0");
            if (scheduledDateTime < DateTime.UtcNow)
                throw new ArgumentException("ScheduledDateTime must be in the future");

            var appointment = await _unitOfWork.Appointments.GetByIdAsync(id);
            if (appointment == null)
                throw new ArgumentException("Appointment not found");

            var doctorSchedule = await _unitOfWork.DoctorSchedules.GetByIdAsync(doctorScheduleId);
            if (doctorSchedule == null)
                throw new ArgumentException("DoctorSchedule not found");

            // Check for conflict: same doctor, same date, same slot (excluding current appointment)
            var conflict = await _unitOfWork.Appointments.GetByDoctorAndScheduleAsync(appointment.DoctorId, scheduledDateTime, doctorScheduleId);
            if (conflict != null && conflict.Id != id)
            {
                throw new InvalidOperationException("Doctor already has an appointment at this time slot.");
            }

            appointment.DoctorScheduleId = doctorScheduleId;
            appointment.ScheduledDateTime = scheduledDateTime;
            appointment.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Appointments.UpdateAsync(appointment);
            await _unitOfWork.SaveChangesAsync();

            // --- TRIGGER LOGIC FOR NOTIFICATIONS AND EMAILS (Reschedule) ---
            try
            {
                var cycle = await _unitOfWork.TreatmentCycles.GetCycleByIdAsync(appointment.CycleId);
                var customer = (cycle != null) ? await _unitOfWork.Customers.GetByIdAsync(cycle.CustomerId) : null;
                var customerUser = (customer != null) ? await _userRepository.GetByIdAsync(customer.UserId) : null;

                var doctor = await _unitOfWork.Doctors.GetDoctorByIdAsync(appointment.DoctorId);
                var doctorUser = await _userRepository.GetByIdAsync(doctor.UserId);

                if (customerUser != null)
                {
                    var customerData = new Dictionary<string, string>
                    {
                        { "CustomerName", customerUser.FullName },
                        { "DoctorName", doctorUser?.FullName ?? "Bác sĩ" },
                        { "AppointmentDateTime", appointment.ScheduledDateTime.ToString("dd/MM/yyyy HH:mm") },
                        { "AppointmentType", appointment.AppointmentType.ToString() }
                    };
                    await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                    {
                        UserId = customerUser.Id,
                        Title = "Lịch hẹn của bạn đã được dời lại",
                        Message = $"Lịch hẹn của bạn với {doctorUser?.FullName ?? "bác sĩ"} đã được dời lại vào lúc {appointment.ScheduledDateTime:dd/MM/yyyy HH:mm}.",
                        Type = NotificationType.Appointment,
                        RelatedEntityType = "Appointment",
                        RelatedEntityId = appointment.Id,
                        SendEmail = true,
                        EmailTemplateName = "AppointmentRescheduleConfirmation",
                        EmailTemplateData = customerData
                    });
                    _logger.LogInformation("Gửi thông báo dời lịch hẹn cho khách hàng {CustomerId}.", customerUser.Id);
                }

                var newReminderTime = appointment.ScheduledDateTime.AddHours(-24);
                if (newReminderTime > DateTime.UtcNow)
                {
                    if (customerUser != null)
                    {
                        var customerData = new Dictionary<string, string>
                            {
                                { "CustomerName", customerUser.FullName },
                                { "DoctorName", doctorUser?.FullName ?? "Bác sĩ" },
                                { "AppointmentDateTime", appointment.ScheduledDateTime.ToString("dd/MM/yyyy HH:mm") },
                                { "AppointmentType", appointment.AppointmentType.ToString() }
                            };
                        await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                        {
                            UserId = customerUser.Id,
                            Title = "Nhắc nhở lịch hẹn sắp tới (đã dời)",
                            Message = $"Lịch hẹn của bạn với {doctorUser?.FullName ?? "bác sĩ"} đã được dời và sẽ diễn ra vào lúc {appointment.ScheduledDateTime:dd/MM/yyyy HH:mm} ngày mai.",
                            Type = NotificationType.Reminder,
                            RelatedEntityType = "Appointment",
                            RelatedEntityId = appointment.Id,
                            ScheduledAt = newReminderTime,
                            SendEmail = true,
                            EmailTemplateName = "AppointmentReminder",
                            EmailTemplateData = customerData
                        });
                        _logger.LogInformation("Đã lên lịch nhắc nhở lịch hẹn dời lại cho khách hàng {CustomerId} vào {NewReminderTime}.", customerUser.Id, newReminderTime);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo thông báo cho lịch hẹn đã dời {AppointmentId}.", appointment.Id);
            }

            return new AppointmentResponseDto
            {
                Id = appointment.Id,
                DoctorId = appointment.DoctorId,
                DoctorScheduleId = appointment.DoctorScheduleId,
                AppointmentType = appointment.AppointmentType,
                ScheduledDateTime = appointment.ScheduledDateTime,
                Status = appointment.Status,
                Notes = appointment.Notes,
                Results = appointment.Results
            };
        }

        public async Task<AppointmentResponseDto> CancelAppointmentAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("AppointmentId is required and must be greater than 0");

            var appointment = await _unitOfWork.Appointments.GetByIdAsync(id);
            if (appointment == null) throw new Exception("Appointment not found");

            appointment.Status = AppointmentStatus.Cancelled;
            await _unitOfWork.Appointments.UpdateAsync(appointment);
            await _unitOfWork.SaveChangesAsync();

            // --- TRIGGER LOGIC FOR NOTIFICATIONS AND EMAILS (Cancel) ---
            try
            {
                var cycle = await _unitOfWork.TreatmentCycles.GetCycleByIdAsync(appointment.CycleId);
                var customer = (cycle != null) ? await _unitOfWork.Customers.GetByIdAsync(cycle.CustomerId) : null;
                var customerUser = (customer != null) ? await _userRepository.GetByIdAsync(customer.UserId) : null;

                var doctor = await _unitOfWork.Doctors.GetDoctorByIdAsync(appointment.DoctorId);
                var doctorUser = await _userRepository.GetByIdAsync(doctor.UserId);

                if (customerUser != null)
                {
                    var customerData = new Dictionary<string, string>
                    {
                        { "CustomerName", customerUser.FullName },
                        { "DoctorName", doctorUser?.FullName ?? "Bác sĩ" },
                        { "AppointmentDateTime", appointment.ScheduledDateTime.ToString("dd/MM/yyyy HH:mm") },
                        { "AppointmentType", appointment.AppointmentType.ToString() }
                    };
                    await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                    {
                        UserId = customerUser.Id,
                        Title = "Lịch hẹn của bạn đã bị hủy",
                        Message = $"Lịch hẹn của bạn với {doctorUser?.FullName ?? "bác sĩ"} vào lúc {appointment.ScheduledDateTime:dd/MM/yyyy HH:mm} đã bị hủy.",
                        Type = NotificationType.Appointment,
                        RelatedEntityType = "Appointment",
                        RelatedEntityId = appointment.Id,
                        SendEmail = true,
                        EmailTemplateName = "AppointmentCancellation",
                        EmailTemplateData = customerData
                    });
                    _logger.LogInformation("Gửi thông báo hủy lịch hẹn cho khách hàng {CustomerId}.", customerUser.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo thông báo cho lịch hẹn đã hủy {AppointmentId}.", appointment.Id);
            }

            return new AppointmentResponseDto
            {
                Id = appointment.Id,
                DoctorId = appointment.DoctorId,
                DoctorScheduleId = appointment.DoctorScheduleId,
                AppointmentType = appointment.AppointmentType,
                ScheduledDateTime = appointment.ScheduledDateTime,
                Status = appointment.Status,
                Notes = appointment.Notes,
                Results = appointment.Results
            };
        }

        public async Task<PaginatedResultDto<DoctorScheduleDto>> GetDoctorAvailabilityAsync(int doctorId, DateTime date, PaginationQueryDTO pagination)
        {
            var entityResult = await _unitOfWork.Appointments.GetDoctorAvailabilityAsync(doctorId, date, pagination);
            return new PaginatedResultDto<DoctorScheduleDto>(
                _mapper.Map<List<DoctorScheduleDto>>(entityResult.Items),
                entityResult.TotalCount,
                entityResult.PageNumber,
                entityResult.PageSize
            );
        }

        public async Task<AppointmentResponseDto?> GetByIdAsync(int id)
        {
            var appointment = await _unitOfWork.Appointments.GetByIdAsync(id);
            if (appointment == null) return null;
            return new AppointmentResponseDto
            {
                Id = appointment.Id,
                DoctorId = appointment.DoctorId,
                DoctorScheduleId = appointment.DoctorScheduleId,
                AppointmentType = appointment.AppointmentType,
                ScheduledDateTime = appointment.ScheduledDateTime,
                Status = appointment.Status,
                Notes = appointment.Notes,
                Results = appointment.Results
            };
        }
    }
}
