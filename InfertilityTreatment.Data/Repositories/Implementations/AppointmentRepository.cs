using InfertilityTreatment.Data.Context;
using InfertilityTreatment.Data.Repositories.Interfaces;
using InfertilityTreatment.Entity.DTOs.Appointments;
using InfertilityTreatment.Entity.DTOs.Common;
using InfertilityTreatment.Entity.Entities;
using InfertilityTreatment.Entity.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Data.Repositories.Implementations
{
    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly ApplicationDbContext _context;
        public AppointmentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Appointment> CreateAsync(Appointment appointment)
        {
            appointment.CreatedAt = DateTime.UtcNow;
            appointment.UpdatedAt = DateTime.UtcNow;
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            return appointment;
        }

        public async Task<List<Appointment>> GetAllAsync()
        {
            return await _context.Appointments
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.ScheduledDateTime)
                .ToListAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return false;
            
            appointment.IsActive = false;
            appointment.UpdatedAt = DateTime.UtcNow;
            _context.Appointments.Update(appointment);
            
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<PaginatedResultDto<Appointment>> GetByCustomerAsync(int customerId, PaginationQueryDTO pagination)
        {
            var query = _context.Appointments
                .Where(a => a.CustomerId == customerId);

            var totalCount = await query.CountAsync();

            var pagedAppointments = await query
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            return new PaginatedResultDto<Appointment>(
                pagedAppointments,
                totalCount,
                pagination.PageNumber,
                pagination.PageSize
            );
        }

        public async Task<PaginatedResultDto<Appointment>> GetByDoctorAndDateAsync(int doctorId, DateTime date, PaginationQueryDTO pagination)
        {
            var query = _context.Appointments
                .Where(a => a.DoctorId == doctorId && a.ScheduledDateTime.Date == date.Date);

            var totalCount = await query.CountAsync();

            var appointments = await query
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            return new PaginatedResultDto<Appointment>(
                appointments,
                totalCount,
                pagination.PageNumber,
                pagination.PageSize
            );
        }

        public async Task<List<Appointment>> GetByDoctorAndDateAsync(int doctorId, DateTime date)
        {
            return await _context.Appointments
                .Where(a => a.DoctorId == doctorId && a.ScheduledDateTime.Date == date.Date && a.Status != AppointmentStatus.Cancelled)
                .ToListAsync();
        }

        public async Task<Appointment?> GetByIdAsync(int id)
        {
            return await _context.Appointments.FindAsync(id);
        }

        public async Task<bool> UpdateAsync(Appointment appointment)
        {
            _context.Appointments.Update(appointment);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> CancelAsync(int appointmentId)
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment == null) return false;
            appointment.Status = AppointmentStatus.Cancelled;
            appointment.UpdatedAt = DateTime.UtcNow;
            _context.Appointments.Update(appointment);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<PaginatedResultDto<DoctorSchedule>> GetDoctorAvailabilityAsync(int doctorId, DateTime date, PaginationQueryDTO pagination)
        {
            // Get all DoctorSchedules for the doctor
            var allSchedules = await _context.DoctorSchedules.ToListAsync();
            // Get all booked appointments for the doctor on the date
            var bookedAppointments = await _context.Appointments
                .Where(a => a.DoctorId == doctorId && a.ScheduledDateTime.Date == date.Date && a.Status != AppointmentStatus.Cancelled)
                .ToListAsync();
            var bookedScheduleIds = bookedAppointments.Select(a => a.DoctorScheduleId).ToHashSet();
            var availableSchedules = allSchedules.Where(s => !bookedScheduleIds.Contains(s.Id)).ToList();
            var totalCount = availableSchedules.Count;
            var paginated = availableSchedules.Skip((pagination.PageNumber - 1) * pagination.PageSize).Take(pagination.PageSize).ToList();
            return new PaginatedResultDto<DoctorSchedule>(
                paginated,
                totalCount,
                pagination.PageNumber,
                pagination.PageSize
            );
        }

        public async Task<Appointment?> GetByDoctorAndScheduleAsync(int doctorId, DateTime scheduledDate, int doctorScheduleId)
        {
            return await _context.Appointments.FirstOrDefaultAsync(a =>
                a.DoctorId == doctorId &&
                a.ScheduledDateTime.Date == scheduledDate.Date &&
                a.DoctorScheduleId == doctorScheduleId &&
                a.Status != AppointmentStatus.Cancelled);
        }

        public async Task<List<Appointment>> GetAppointmentsByCycleIdAsync(int cycleId)
        {
            return await _context.Appointments
                .Where(a => a.CycleId == cycleId && a.IsActive)
                .OrderBy(a => a.ScheduledDateTime)
                .ToListAsync();
        }

        // Enhanced methods for availability and conflict checking
        public async Task<Appointment?> GetByDoctorAndTimeRangeAsync(int doctorId, DateTime startTime, DateTime endTime)
        {
            return await _context.Appointments
                .Where(a => a.DoctorId == doctorId && 
                           a.IsActive && 
                           a.Status != AppointmentStatus.Cancelled &&
                           a.ScheduledDateTime >= startTime && 
                           a.ScheduledDateTime < endTime)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Appointment>> GetByDoctorAndDateRangeAsync(int doctorId, DateTime startDate, DateTime endDate)
        {
            return await _context.Appointments
                .Include(a => a.TreatmentCycle)
                .ThenInclude(c => c.Customer)
                .Where(a => a.DoctorId == doctorId && 
                           a.IsActive && 
                           a.Status != AppointmentStatus.Cancelled &&
                           a.ScheduledDateTime.Date >= startDate.Date && 
                           a.ScheduledDateTime.Date <= endDate.Date)
                .OrderBy(a => a.ScheduledDateTime)
                .ToListAsync();
        }

        public async Task<List<Appointment>> GetOverlappingAppointmentsAsync(int doctorId, DateTime startTime, DateTime endTime)
        {
            return await _context.Appointments
                .Include(a => a.TreatmentCycle)
                .ThenInclude(c => c.Customer)
                .Where(a => a.DoctorId == doctorId && 
                           a.IsActive && 
                           a.Status != AppointmentStatus.Cancelled &&
                           ((a.ScheduledDateTime >= startTime && a.ScheduledDateTime < endTime) ||
                            (a.ScheduledDateTime.AddHours(1) > startTime && a.ScheduledDateTime.AddHours(1) <= endTime) ||
                            (a.ScheduledDateTime <= startTime && a.ScheduledDateTime.AddHours(1) >= endTime)))
                .OrderBy(a => a.ScheduledDateTime)
                .ToListAsync();
        }
    }

}
