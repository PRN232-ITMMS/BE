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
        Task<PaginatedResultDto<CycleResponseDto>> GetCyclesByCustomerAsync(int userId, TreatmentCycleFilterDto filter);
        Task<PaginatedResultDto<CycleResponseDto>> GetCyclesByDoctorAsync(int userId, TreatmentCycleFilterDto filter);
        Task<CycleDetailDto> GetCycleByIdAsync(int cycleId);
        Task<bool> UpdateCycleStatusAsync(int cycleId, CycleStatus status);
        Task<bool> AssignDoctorToCycleAsync(int cycleId, int doctorId);
        Task<decimal> CalculateCycleCostAsync(int cycleId);
        Task<PhaseResponseDto> AddPhaseAsync(int cycleId, CreatePhaseDto createPhaseDto);
        Task<bool> UpdatePhaseAsync(int phaseId, UpdatePhaseDto updatePhaseDto);
        Task<PaginatedResultDto<PhaseResponseDto>> GetCyclePhasesAsync(int cycleId, TreatmentPhaseFilterDto filter);
        Task<bool> UpdateCycleAsync(int cycleId, UpdateCycleDto dto);
        
        // New phase management methods
        Task<PhaseResponseDto> StartPhaseAsync(int cycleId, int phaseId, StartPhaseDto dto);
        Task<PhaseResponseDto> CompletePhaseAsync(int cycleId, int phaseId, CompletePhaseDto dto);
        Task<PhaseProgressDto> GetPhaseProgressAsync(int cycleId, int phaseId);
        Task<List<PhaseResponseDto>> GenerateDefaultPhasesAsync(int cycleId, GeneratePhasesDto dto);
        
        // New cycle initialization workflow methods
        Task<CycleResponseDto> InitializeCycleAsync(int cycleId, InitializeCycleDto dto);
        Task<CycleResponseDto> StartTreatmentAsync(int cycleId, StartTreatmentDto dto);
        Task<CycleTimelineDto> GetCycleTimelineAsync(int cycleId);
    }
}
