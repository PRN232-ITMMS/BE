using InfertilityTreatment.Entity.DTOs.Common;
using InfertilityTreatment.Entity.DTOs.DoctorSchedules;
using InfertilityTreatment.Entity.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InfertilityTreatment.Data.Repositories.Interfaces
{
    public interface IDoctorScheduleRepository
    {
        Task<DoctorSchedule?> GetByIdAsync(int id);
        Task<PaginatedResultDto<DoctorSchedule>> GetByDoctorIdAsync(int doctorId, PaginationQueryDTO pagination);
        Task<DoctorSchedule> CreateAsync(DoctorSchedule dto);
        Task<bool> UpdateAsync(DoctorSchedule dto);
        Task<bool> DeleteAsync(int id);
        Task<List<DoctorSchedule>> GetSchedulesByDoctorAndDateAsync(int doctorId, DateTime date);
        Task<List<DoctorSchedule>> GetDoctorSchedulesByDateAsync(int doctorId, DateTime date);

        Task<PaginatedResultDto<DoctorSchedule>> GetAll(PaginationQueryDTO pagination);
    }
}
