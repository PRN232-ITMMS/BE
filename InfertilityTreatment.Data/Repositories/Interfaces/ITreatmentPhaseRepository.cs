using InfertilityTreatment.Entity.DTOs.Common;
using InfertilityTreatment.Entity.DTOs.TreatmentPhase;
using InfertilityTreatment.Entity.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Data.Repositories.Interfaces
{
    public interface ITreatmentPhaseRepository
    {
        // Basic CRUD Operations
        Task<List<TreatmentPhase>> GetAllAsync();
        Task<TreatmentPhase?> GetByIdAsync(int id);
        Task<TreatmentPhase> AddTreatmentPhaseAsync(TreatmentPhase treatmentPhase);
        Task<bool> UpdatePhaseAsync(int phaseId, TreatmentPhase treatmentPhase);
        Task<bool> DeleteAsync(int id);
        
        // Specialized Operations
        Task<PaginatedResultDto<TreatmentPhase>> GetCyclePhasesByCycleId(int cycleId, TreatmentPhaseFilterDto filter);
    }
}
