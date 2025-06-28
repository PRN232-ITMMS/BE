using InfertilityTreatment.Entity.DTOs.Appointments;
using InfertilityTreatment.Entity.DTOs.Common;
using InfertilityTreatment.Entity.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Data.Repositories.Interfaces
{
   public interface IAppointmentRepository
    {
        // Basic CRUD Operations
        Task<List<Appointment>> GetAllAsync();
        Task<Appointment?> GetByIdAsync(int id);
        Task<Appointment> CreateAsync(Appointment appointment);
        Task<bool> UpdateAsync(Appointment appointment);
        Task<bool> CancelAsync(int appointmentId);
        Task<bool> DeleteAsync(int id);
        
        // Specialized Operations
        Task<PaginatedResultDto<Appointment>> GetByCustomerAsync(int customerId, PaginationQueryDTO pagination);
        Task<PaginatedResultDto<Appointment>> GetByDoctorAndDateAsync(int doctorId, DateTime date, PaginationQueryDTO pagination);
        Task<List<Appointment>> GetByDoctorAndDateAsync(int doctorId, DateTime date);
        Task<PaginatedResultDto<DoctorSchedule>> GetDoctorAvailabilityAsync(int doctorId, DateTime date, PaginationQueryDTO pagination);
        Task<Appointment?> GetByDoctorAndScheduleAsync(int doctorId, DateTime scheduledDate, int doctorScheduleId);
    }
}
