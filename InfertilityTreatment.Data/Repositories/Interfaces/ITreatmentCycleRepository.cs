using InfertilityTreatment.Entity.DTOs.Common;
using InfertilityTreatment.Entity.DTOs.TreatmentCycles;
using InfertilityTreatment.Entity.Entities;
using InfertilityTreatment.Entity.Enums;

namespace InfertilityTreatment.Data.Repositories.Interfaces
{
    public interface ITreatmentCycleRepository : IBaseRepository<TreatmentCycle>
    {
        Task<PaginatedResultDto<TreatmentCycle>> GetCyclesByCustomerAsync(int customerId, TreatmentCycleFilterDto filter);
        Task<PaginatedResultDto<TreatmentCycle>> GetCyclesByDoctorAsync(int doctorId, TreatmentCycleFilterDto filter);
        Task<IEnumerable<TreatmentCycle>> GetCyclesByStatusAsync(CycleStatus status);
        Task<bool> UpdateTreatmentCycleAsync(TreatmentCycle treatmentCycle);
        Task<TreatmentCycle?> GetCycleWithDetailsAsync(int cycleId);
        Task<TreatmentCycle> AddTreatmentCycleAsync(TreatmentCycle treatmentCycle);
        Task<TreatmentCycle> GetCycleByIdAsync(int cycleId);
        Task<bool> UpdateStatusAsync(int cycleId, CycleStatus status);
        Task<bool> UpdateDoctorAsync(int cycleId, int doctorId);
        Task<decimal> CalculatePhaseCostAsync(int cycleId);
    }
}