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
        Task<TreatmentPhase> AddTreatmentPhaseAsync(TreatmentPhase treatmentPhase);
        Task<PaginatedResultDto<TreatmentPhase>> GetCyclePhasesByCycleId(int cycleId, TreatmentPhaseFilterDto filter);
        Task<bool> UpdatePhaseAsync(int phaseId, TreatmentPhase treatmentPhase);
    }
}
