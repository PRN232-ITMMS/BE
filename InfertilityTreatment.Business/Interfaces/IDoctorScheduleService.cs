using InfertilityTreatment.Entity.DTOs.Common;
using InfertilityTreatment.Entity.DTOs.DoctorSchedules;
using System.Threading.Tasks;

namespace InfertilityTreatment.Business.Interfaces
{
    public interface IDoctorScheduleService
    {
        Task<DoctorScheduleDto?> GetByIdAsync(int id);
        Task<PaginatedResultDto<DoctorScheduleDto>> GetAllAsync(PaginationQueryDTO pagination);
        Task<PaginatedResultDto<DoctorScheduleDto>> GetByDoctorIdAsync(int doctorId, PaginationQueryDTO pagination);
        Task<PaginatedResultDto<DoctorScheduleDto>> GetAvailableSlotsAsync(int doctorId, DateTime date, PaginationQueryDTO pagination);
        Task<DoctorScheduleDto> CreateAsync(CreateDoctorScheduleDto dto);
        Task<DoctorScheduleDto> UpdateAsync(int id, UpdateDoctorScheduleDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
