using InfertilityTreatment.Data.Configurations;
using InfertilityTreatment.Data.Context;
using InfertilityTreatment.Data.Repositories.Interfaces;
using InfertilityTreatment.Entity.DTOs.Common;
using InfertilityTreatment.Entity.DTOs.TreatmentPhase;
using InfertilityTreatment.Entity.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Data.Repositories.Implementations
{
    public class TreatmentPhaseRepository : BaseRepository<TreatmentPhase>, ITreatmentPhaseRepository
    {
        public TreatmentPhaseRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<TreatmentPhase> AddTreatmentPhaseAsync(TreatmentPhase treatmentPhase)
        {
            _context.TreatmentPhases.Add(treatmentPhase);
            await _context.SaveChangesAsync();
            return treatmentPhase;
        }


        public async Task<PaginatedResultDto<TreatmentPhase>> GetCyclePhasesByCycleId(int cycleId, TreatmentPhaseFilterDto filter)
        {
            var query = _context.TreatmentPhases
                .Where(tp => tp.CycleId == cycleId && tp.IsActive)
                .OrderBy(tp => tp.PhaseOrder)
                .AsQueryable();

            var totalCount = await query.CountAsync();

            var pagedData = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PaginatedResultDto<TreatmentPhase>(
                pagedData,
                totalCount,
                filter.PageNumber,
                filter.PageSize
            );
        }


        public async Task<bool> UpdatePhaseAsync(int phaseId, TreatmentPhase treatmentPhase)
        {
            var existingPhase = await _context.TreatmentPhases.FirstOrDefaultAsync(tp => tp.Id == phaseId);

            if (existingPhase == null)
            {
                throw new Exception("Treatment phase not found.");
            }
            existingPhase.PhaseName = treatmentPhase.PhaseName;
            existingPhase.PhaseOrder = treatmentPhase.PhaseOrder;
            existingPhase.Status = treatmentPhase.Status;
            existingPhase.StartDate = treatmentPhase.StartDate;
            existingPhase.EndDate = treatmentPhase.EndDate;
            existingPhase.Cost = treatmentPhase.Cost;
            existingPhase.Instructions = treatmentPhase.Instructions;
            existingPhase.Notes = treatmentPhase.Notes;
            existingPhase.UpdatedAt = DateTime.UtcNow;
            existingPhase.IsActive = treatmentPhase.IsActive;

            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

    }
}
