using InfertilityTreatment.Entity.DTOs.Common;
using InfertilityTreatment.Entity.DTOs.TreatmentCycles;
using InfertilityTreatment.Entity.DTOs.TreatmentPhase;
using InfertilityTreatment.Entity.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Business.Interfaces
{
    public interface ICycleService
    {
        Task<CycleResponseDto> CreateCycleAsync(CreateCycleDto createCycleDto);
        Task<PaginatedResultDto<CycleResponseDto>> GetCyclesByCustomerAsync(int customerId, TreatmentCycleFilterDto filter);
        Task<PaginatedResultDto<CycleResponseDto>> GetCyclesByDoctorAsync(int doctorId, TreatmentCycleFilterDto filter);
        Task<CycleDetailDto> GetCycleByIdAsync(int cycleId);
        Task<bool> UpdateCycleStatusAsync(int cycleId, CycleStatus status);
        Task<bool> AssignDoctorToCycleAsync(int cycleId, int doctorId);
        Task<decimal> CalculateCycleCostAsync(int cycleId);
        Task<PhaseResponseDto> AddPhaseAsync(int cycleId, CreatePhaseDto createPhaseDto);
        Task<bool> UpdatePhaseAsync(int phaseId, UpdatePhaseDto updatePhaseDto);
        Task<PaginatedResultDto<PhaseResponseDto>> GetCyclePhasesAsync(int cycleId, TreatmentPhaseFilterDto filter);
        Task<bool> UpdateCycleAsync(int cycleId, UpdateCycleDto dto);
    }
}
