using AutoMapper;
using InfertilityTreatment.Business.Interfaces;
using InfertilityTreatment.Data.Repositories.Implementations;
using InfertilityTreatment.Data.Repositories.Interfaces;
using InfertilityTreatment.Entity.Constants;
using InfertilityTreatment.Entity.DTOs.Appointments;
using InfertilityTreatment.Entity.DTOs.Common;
using InfertilityTreatment.Entity.DTOs.DoctorSchedules;
using InfertilityTreatment.Entity.DTOs.Email;
using InfertilityTreatment.Entity.DTOs.Notifications;
using InfertilityTreatment.Entity.DTOs.TreatmentPakages;
using InfertilityTreatment.Entity.Entities;
using InfertilityTreatment.Entity.Enums;
using Microsoft.EntityFrameworkCore;
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
        private readonly IRealTimeNotificationService _realTimeNotificationService;
        private readonly IEmailService _emailService;

        public AppointmentService(IUnitOfWork unitOfWork, IMapper mapper, INotificationService notificationService, IRealTimeNotificationService realTimeNotificationService, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _notificationService = notificationService;
            _realTimeNotificationService = realTimeNotificationService;
            _emailService = emailService;
        }

        public async Task<AppointmentResponseDto> CreateAppointmentAsync(CreateAppointmentDto dto)
        {

                // Check existence of related entities
                
                var doctor = await _unitOfWork.Doctors.GetDoctorByIdAsync(dto.DoctorId);
                if (doctor == null)
                    throw new ArgumentException("Doctor not found");
                var doctorSchedule = await _unitOfWork.DoctorSchedules.GetByIdAsync(dto.DoctorScheduleId);
                if (doctorSchedule == null)
                    throw new ArgumentException("DoctorSchedule not found");
                var customer = await _unitOfWork.Customers.GetByIdAsync(dto.CustomerId);
                if (customer == null)
                throw new ArgumentException("Customer not found");

            // Check for conflict: same doctor, same date, same slot
            var conflict = await _unitOfWork.Appointments.GetByDoctorAndScheduleAsync(dto.DoctorId, dto.ScheduledDateTime, dto.DoctorScheduleId);
                if (conflict != null)
                {
                    throw new InvalidOperationException("Doctor already has an appointment at this time slot.");
                }

                var appointment = new Appointment
                {
                    CycleId = dto?.CycleId,
                    CustomerId = dto.CustomerId,
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

                var result = new AppointmentResponseDto
                {
                    Id = created.Id,
                    CycleId = created?.CycleId,
                    CustomerId = created.CustomerId,
                    DoctorId = created.DoctorId,
                    DoctorScheduleId = created.DoctorScheduleId,
                    AppointmentType = created.AppointmentType,
                    ScheduledDateTime = created.ScheduledDateTime,
                    Status = created.Status,
                    Notes = created.Notes,
                    Results = created.Results
                };

                // Send notification for appointment creation
                await SendAppointmentCreatedNotificationAsync(result);

                // Send email confirmation
                await SendAppointmentEmailConfirmationAsync(result);

                return result;
            
            
        }

        public async Task<PaginatedResultDto<AppointmentResponseDto>> GetAppointmentsByCustomerAsync(int userId, PaginationQueryDTO pagination)
        {
            var cus = await _unitOfWork.Customers.GetByUserIdAsync(userId);
            if (cus == null)
            {
                return new PaginatedResultDto<AppointmentResponseDto>(
                [],
                0,
                pagination.PageNumber,
                pagination.PageSize
            );
            }
            var list = await _unitOfWork.Appointments.GetByCustomerAsync(cus.Id, pagination);
            var dtoList = list.Items.Select(a => new AppointmentResponseDto
            {
                Id = a.Id,
                DoctorId = a.DoctorId,
                CycleId = a.CycleId,
                CustomerId = a.CustomerId,
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

        public async Task<PaginatedResultDto<AppointmentResponseDto>> GetAppointmentsByDoctorAsync(int userId, DateTime date, PaginationQueryDTO pagination)
        {
            var doc = await _unitOfWork.Doctors.GetByUserIdAsync(userId);
            if (doc == null)
            {
                return new PaginatedResultDto<AppointmentResponseDto>(
                [],
                0,
                pagination.PageNumber,
                pagination.PageSize
            );
            }
            var list = await _unitOfWork.Appointments.GetByDoctorAndDateAsync(doc.Id, date, pagination);
            var dtoList = list.Items.Select(a => new AppointmentResponseDto
            {
                Id = a.Id,
                CycleId = a.CycleId,
                CustomerId = a.CustomerId,
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
                throw new InvalidOperationException("Appointment not found");

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

            var result = new AppointmentResponseDto
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

            // Send notification for appointment reschedule
            await SendAppointmentRescheduledNotificationAsync(result);

            return result;
        }

        public async Task<AppointmentResponseDto> CancelAppointmentAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("AppointmentId is required and must be greater than 0");
            
                var appointment = await _unitOfWork.Appointments.GetByIdAsync(id);
                if (appointment == null) throw new InvalidOperationException("Appointment not found");

                appointment.Status = AppointmentStatus.Cancelled;
                await _unitOfWork.Appointments.UpdateAsync(appointment);
                await _unitOfWork.SaveChangesAsync();

                var result = new AppointmentResponseDto
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

                // Send notification for appointment cancellation
                await SendAppointmentCancelledNotificationAsync(result);

                return result;
            
            
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
                CycleId = appointment?.CycleId,
                CustomerId = appointment.CustomerId,
                DoctorId = appointment.DoctorId,
                DoctorScheduleId = appointment.DoctorScheduleId,
                AppointmentType = appointment.AppointmentType,
                ScheduledDateTime = appointment.ScheduledDateTime,
                Status = appointment.Status,
                Notes = appointment.Notes,
                Results = appointment.Results
            };
        }

        // Enhanced Appointment Features
        

        public async Task<BulkCreateResultDto> CreateBulkAppointmentsAsync(BulkCreateAppointmentsDto dto)
        {
            var result = new BulkCreateResultDto
            {
                TotalRequested = dto.Appointments.Count
            };

            // Begin transaction for bulk operation
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                for (int i = 0; i < dto.Appointments.Count; i++)
                {
                    try
                    {
                        var appointment = dto.Appointments[i];

                        // Check conflicts if enabled
                        if (dto.CheckConflicts)
                        {
                            var existingAppointment = await _unitOfWork.Appointments
                                .GetByDoctorAndScheduleAsync(appointment.DoctorId, appointment.ScheduledDateTime, appointment.DoctorScheduleId);

                            if (existingAppointment != null)
                            {
                                result.Conflicts.Add(new ConflictDto
                                {
                                    AppointmentIndex = i,
                                    ConflictType = "Time Conflict",
                                    ConflictReason = "Doctor already has an appointment at this time",
                                    ConflictTime = appointment.ScheduledDateTime,
                                    ExistingAppointment = _mapper.Map<AppointmentResponseDto>(existingAppointment)
                                });

                                if (!dto.ContinueOnError)
                                {
                                    result.Failed++;
                                    continue;
                                }
                            }
                        }

                        var createdAppointment = await CreateAppointmentAsync(appointment);
                        result.CreatedAppointments.Add(createdAppointment);
                        result.SuccessfullyCreated++;

                        // Send notifications if enabled
                        //if (dto.SendNotifications)
                        //{
                        //    await SendAppointmentCreatedNotificationAsync(createdAppointment);
                        //}
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Appointment {i}: {ex.Message}");
                        result.Failed++;

                        if (!dto.ContinueOnError)
                        {
                            await _unitOfWork.RollbackTransactionAsync();
                            throw;
                        }
                    }
                }

                // Commit transaction if successful
                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception)
            {
                // Rollback transaction on any unhandled error
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            return result;
        }

        public async Task<AutoScheduleResultDto> AutoScheduleAppointmentsAsync(AutoScheduleDto dto)
        {
            var result = new AutoScheduleResultDto
            {
                CycleId = dto.CycleId,
                DoctorId = dto.DoctorId,
                TotalPlanned = dto.AppointmentTypes.Count
            };

            var cycle = await _unitOfWork.TreatmentCycles.GetCycleByIdAsync(dto.CycleId);
            if (cycle == null)
            {
                result.Errors.Add("Treatment cycle not found");
                return result;
            }

            var doctor = await _unitOfWork.Doctors.GetDoctorByIdAsync(dto.DoctorId);
            if (doctor == null)
            {
                result.Errors.Add("Doctor not found");
                return result;
            }

            // Begin transaction for auto-scheduling
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var currentDate = dto.PreferredStartDate;
                result.NextAvailableDate = currentDate;

                // Batch load doctor schedules for the entire date range to improve performance
                var endDate = currentDate.AddDays(30);
                var allDoctorSchedules = await GetDoctorSchedulesInDateRange(dto.DoctorId, currentDate, endDate);

                foreach (var appointmentType in dto.AppointmentTypes)
                {
                    try
                    {
                        // Parse appointment type string to enum
                        if (!Enum.TryParse<AppointmentType>(appointmentType, true, out var parsedAppointmentType))
                        {
                            result.Errors.Add($"Invalid appointment type: {appointmentType}");
                            continue;
                        }

                        // Find next available slot using cached schedules
                        var nextSlot = await FindNextAvailableSlot(dto.DoctorId, currentDate, endDate, 
                            dto.DefaultDuration, dto.PreferredTimeStart, dto.PreferredTimeEnd, allDoctorSchedules);

                        if (nextSlot != null)
                        {
                            var appointmentDto = new CreateAppointmentDto
                            {
                                CycleId = dto.CycleId,
                                DoctorId = dto.DoctorId,
                                DoctorScheduleId = nextSlot.ScheduleId,
                                ScheduledDateTime = nextSlot.StartTime,
                                AppointmentType = parsedAppointmentType,
                                Notes = $"Auto-scheduled {appointmentType} appointment"
                            };

                            var createdAppointment = await CreateAppointmentAsync(appointmentDto);
                            result.ScheduledAppointments.Add(createdAppointment);
                            result.SuccessfullyScheduled++;

                            // Send notifications if enabled
                            //if (dto.SendNotifications)
                            //{
                            //    await SendAppointmentCreatedNotificationAsync(createdAppointment);
                            //}

                            currentDate = nextSlot.StartTime.AddDays(dto.DaysBetweenAppointments);
                            result.NextAvailableDate = currentDate;
                        }
                        else
                        {
                            result.Errors.Add($"No available slot found for {appointmentType}");
                            result.Failed++;
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Error scheduling {appointmentType}: {ex.Message}");
                        result.Failed++;
                    }
                }

                // Commit transaction if successful
                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception)
            {
                // Rollback transaction on any error
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            return result;
        }

        public async Task<List<ConflictDto>> GetScheduleConflictsAsync(ConflictCheckDto query)
        {
            var conflicts = new List<ConflictDto>();

            var appointments = await _unitOfWork.Appointments
                .GetByDoctorAndDateRangeAsync(query.DoctorId, query.StartDate, query.EndDate);

            for (int i = 0; i < appointments.Count; i++)
            {
                var current = appointments[i];

                // Check for overlapping appointments
                if (query.IncludeOverlapping)
                {
                    var overlapping = appointments.Where(a => 
                        a.Id != current.Id &&
                        a.ScheduledDateTime < current.ScheduledDateTime.AddHours(1) // Assuming 1 hour duration
                        && a.ScheduledDateTime.AddHours(1) > current.ScheduledDateTime)
                        .ToList();

                    foreach (var overlap in overlapping)
                    {
                        conflicts.Add(new ConflictDto
                        {
                            ConflictType = "Overlapping",
                            ConflictReason = "Appointments overlap in time",
                            ConflictTime = current.ScheduledDateTime,
                            ExistingAppointment = _mapper.Map<AppointmentResponseDto>(overlap)
                        });
                    }
                }

                // Check for back-to-back appointments
                if (query.IncludeBackToBack)
                {
                    var backToBack = appointments.Where(a =>
                        a.Id != current.Id &&
                        Math.Abs((a.ScheduledDateTime - current.ScheduledDateTime.AddHours(1)).TotalMinutes) < 15)
                        .ToList();

                    foreach (var btb in backToBack)
                    {
                        conflicts.Add(new ConflictDto
                        {
                            ConflictType = "Back-to-Back",
                            ConflictReason = "Insufficient time between appointments",
                            ConflictTime = current.ScheduledDateTime,
                            ExistingAppointment = _mapper.Map<AppointmentResponseDto>(btb)
                        });
                    }
                }
            }

            return conflicts.Distinct().ToList();
        }

        public async Task<bool> SendAppointmentReminderAsync(int appointmentId)
        {
            try
            {
                var appointment = await _unitOfWork.Appointments.GetByIdAsync(appointmentId);
                if (appointment == null)
                    return false;

                // Get cycle with customer information
                var cycle = appointment.CycleId == null ? null : await _unitOfWork.TreatmentCycles.GetCycleByIdAsync(appointment.CycleId.Value);
                var cus = await _unitOfWork.Customers.GetByIdAsync(cycle.CustomerId);

                var reminderDto = new CreateNotificationDto
                {
                    UserId = cus.UserId,
                    Title = "Appointment Reminder",
                    Message = $"You have an appointment scheduled for {appointment.ScheduledDateTime:MMM dd, yyyy} at {appointment.ScheduledDateTime:HH:mm}",
                    Type = NotificationType.Appointment.ToString(),
                    ScheduledAt = DateTime.UtcNow,
                    RelatedEntityId = appointmentId,
                    RelatedEntityType = "Appointment"
                };

                // Save notification to database
                await _notificationService.CreateNotificationAsync(reminderDto);
                
                // Send email reminder
                var appointmentDto = new AppointmentResponseDto
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
                await SendAppointmentReminderEmailAsync(appointmentDto);
                
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Helper method for sending appointment created notifications
        private async Task SendAppointmentCreatedNotificationAsync(AppointmentResponseDto appointment)
        {
            try
            {
                // Get cycle with customer information
                var cycle = appointment.CycleId == null ? null : await _unitOfWork.TreatmentCycles.GetCycleByIdAsync(appointment.CycleId.Value);
                var cus = await _unitOfWork.Customers.GetByIdAsync(appointment.CustomerId);

                var notificationDto = new CreateNotificationDto
                {
                    UserId = cus.UserId,
                    Title = "New Appointment Scheduled",
                    Message = $"Your {appointment.AppointmentType} appointment has been scheduled for {appointment.ScheduledDateTime:MMM dd, yyyy}",
                    Type = NotificationType.Appointment.ToString(),
                    ScheduledAt = DateTime.UtcNow,
                    RelatedEntityId = appointment.Id,
                    RelatedEntityType = "Appointment"
                };

                // Save notification to database
                await _notificationService.CreateNotificationAsync(notificationDto);
                
                
            }
            catch (Exception)
            {
                // Log error but don't fail the appointment creation
            }
        }

        // Helper method for sending appointment rescheduled notifications
        private async Task SendAppointmentRescheduledNotificationAsync(AppointmentResponseDto appointment)
        {
            try
            {
                // Get cycle with customer information
                var cycle = appointment.CycleId == null ? null : await _unitOfWork.TreatmentCycles.GetCycleByIdAsync(appointment.CycleId.Value);
                var cus = await _unitOfWork.Customers.GetByIdAsync(appointment.CustomerId);

                var notificationDto = new CreateNotificationDto
                {
                    UserId = cus.UserId,
                    Title = "Appointment Rescheduled",
                    Message = $"Your {appointment.AppointmentType} appointment has been rescheduled to {appointment.ScheduledDateTime:MMM dd, yyyy} at {appointment.ScheduledDateTime:HH:mm}",
                    Type = NotificationType.Appointment.ToString(),
                    ScheduledAt = DateTime.UtcNow,
                    RelatedEntityId = appointment.Id,
                    RelatedEntityType = "Appointment"
                };

                // Save notification to database
                await _notificationService.CreateNotificationAsync(notificationDto);
                
                
            }
            catch (Exception)
            {
                // Log error but don't fail the appointment operation
            }
        }

        // Helper method for sending appointment cancelled notifications
        private async Task SendAppointmentCancelledNotificationAsync(AppointmentResponseDto appointment)
        {
            try
            {
                // Get cycle with customer information
                var cycle = appointment.CycleId == null ? null : await _unitOfWork.TreatmentCycles.GetCycleByIdAsync(appointment.CycleId.Value);
                var cus = await _unitOfWork.Customers.GetByIdAsync(cycle.CustomerId);

                var notificationDto = new CreateNotificationDto
                {
                    UserId = cus.UserId,
                    Title = "Appointment Cancelled",
                    Message = $"Your {appointment.AppointmentType} appointment scheduled for {appointment.ScheduledDateTime:MMM dd, yyyy} at {appointment.ScheduledDateTime:HH:mm} has been cancelled",
                    Type = NotificationType.Appointment.ToString(),
                    ScheduledAt = DateTime.UtcNow,
                    RelatedEntityId = appointment.Id,
                    RelatedEntityType = "Appointment"
                };

                // Save notification to database
                await _notificationService.CreateNotificationAsync(notificationDto);
                
                
            }
            catch (Exception)
            {
                // Log error but don't fail the appointment operation
            }
        }

        // Helper method for sending appointment email confirmations
        private async Task SendAppointmentEmailConfirmationAsync(AppointmentResponseDto appointment)
        {
            try
            {
                // Get cycle with customer information
                var cycle = appointment.CycleId == null ? null : await _unitOfWork.TreatmentCycles.GetCycleByIdAsync(appointment.CycleId.Value);
                var customer = await _unitOfWork.Customers.GetWithUserAsync(cycle.CustomerId);

                if (!string.IsNullOrEmpty(customer.User?.Email))
                {
                    var confirmationDto = new SendAppointmentConfirmationDto
                    {
                        AppointmentId = appointment.Id,
                        Email = customer.User.Email
                    };


                    await _emailService.SendAppointmentConfirmationAsync(confirmationDto);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the appointment creation
                // Email failures should not prevent appointment operations
            }
        }

        // Helper method for sending appointment reminder emails
        private async Task SendAppointmentReminderEmailAsync(AppointmentResponseDto appointment)
        {
            try
            {
                // Get cycle with customer information
                var cycle = appointment.CycleId == null ? null : await _unitOfWork.TreatmentCycles.GetCycleByIdAsync(appointment.CycleId.Value);
                var customer = await _unitOfWork.Customers.GetWithUserAsync(cycle.CustomerId);

                if (!string.IsNullOrEmpty(customer.User?.Email))
                {
                    var reminderDto = new SendReminderDto
                    {
                        AppointmentId = appointment.Id,
                        Email = customer.User.Email
                    };

                    await _emailService.SendAppointmentReminderAsync(reminderDto);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the operation
            }
        }

        // Performance optimization helper methods for auto-scheduling

        /// <summary>
        /// Batch load doctor schedules for a date range to avoid N+1 queries
        /// </summary>
        private async Task<Dictionary<DateTime, List<DoctorSchedule>>> GetDoctorSchedulesInDateRange(int doctorId, DateTime startDate, DateTime endDate)
        {
            var schedulesByDate = new Dictionary<DateTime, List<DoctorSchedule>>();
            var currentDate = startDate.Date;

            while (currentDate <= endDate.Date)
            {
                var dailySchedules = await _unitOfWork.DoctorSchedules
                    .GetDoctorSchedulesByDateAsync(doctorId, currentDate);
                schedulesByDate[currentDate] = dailySchedules.ToList();
                currentDate = currentDate.AddDays(1);
            }

            return schedulesByDate;
        }

        /// <summary>
        /// Find next available slot using pre-loaded schedules for better performance
        /// </summary>
        private async Task<AvailableSlotInfo?> FindNextAvailableSlot(
            int doctorId, 
            DateTime startDate, 
            DateTime endDate, 
            int duration, 
            TimeSpan preferredTimeStart, 
            TimeSpan preferredTimeEnd,
            Dictionary<DateTime, List<DoctorSchedule>> allSchedules)
        {
            var currentDate = startDate.Date;

            while (currentDate <= endDate.Date)
            {
                if (allSchedules.TryGetValue(currentDate, out var daySchedules))
                {
                    foreach (var schedule in daySchedules)
                    {
                        var slotStart = currentDate.Add(schedule.StartTime);
                        var slotEnd = currentDate.Add(schedule.EndTime);

                        // Check if slot falls within preferred time range
                        if (slotStart.TimeOfDay < preferredTimeStart || slotEnd.TimeOfDay > preferredTimeEnd)
                            continue;

                        // Generate time slots based on duration
                        while (slotStart.AddMinutes(duration) <= slotEnd)
                        {
                            // Check if this time slot is available
                            var existingAppointment = await _unitOfWork.Appointments
                                .GetByDoctorAndTimeRangeAsync(doctorId, slotStart, slotStart.AddMinutes(duration));

                            if (existingAppointment == null)
                            {
                                return new AvailableSlotInfo
                                {
                                    StartTime = slotStart,
                                    EndTime = slotStart.AddMinutes(duration),
                                    ScheduleId = schedule.Id
                                };
                            }

                            slotStart = slotStart.AddMinutes(duration + 15); // 15 minutes buffer
                        }
                    }
                }

                currentDate = currentDate.AddDays(1);
            }

            return null;
        }

        /// <summary>
        /// Helper class for available slot information
        /// </summary>
        private class AvailableSlotInfo
        {
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public int ScheduleId { get; set; }
        }
    }

}
