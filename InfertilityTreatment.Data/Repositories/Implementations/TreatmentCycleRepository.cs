using InfertilityTreatment.Data.Context;
using InfertilityTreatment.Data.Repositories.Interfaces;
using InfertilityTreatment.Entity.DTOs.Common;
using InfertilityTreatment.Entity.DTOs.TreatmentCycles;
using InfertilityTreatment.Entity.Entities;
using InfertilityTreatment.Entity.Enums;
using Microsoft.EntityFrameworkCore;

namespace InfertilityTreatment.Data.Repositories.Implementations
{
    public class TreatmentCycleRepository : BaseRepository<TreatmentCycle>, ITreatmentCycleRepository
    {
        public TreatmentCycleRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<PaginatedResultDto<TreatmentCycle>> GetCyclesByCustomerAsync(int customerId, TreatmentCycleFilterDto filter)
        {
            var query = _context.TreatmentCycles
        .Where(tc => tc.CustomerId == customerId && tc.IsActive)
        .OrderByDescending(tc => tc.CreatedAt)
        .AsQueryable();

            var totalCount = await query.CountAsync();

            var pagedData = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PaginatedResultDto<TreatmentCycle>(pagedData, totalCount, filter.PageNumber, filter.PageSize);
        }


        public async Task<PaginatedResultDto<TreatmentCycle>> GetCyclesByDoctorAsync(int doctorId, TreatmentCycleFilterDto filter)
        {
            var query = _context.TreatmentCycles
          .Include(tc => tc.Customer).ThenInclude(c => c.User)
          .Include(tc => tc.Doctor).ThenInclude(d => d.User)
          .Include(tc => tc.TreatmentPackage)
          .Where(tc => tc.DoctorId == doctorId && tc.IsActive)
          .OrderByDescending(tc => tc.CreatedAt)
          .AsQueryable();

            var totalCount = await query.CountAsync();

            var pagedData = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PaginatedResultDto<TreatmentCycle>(pagedData, totalCount, filter.PageNumber, filter.PageSize);
        }

        public async Task<IEnumerable<TreatmentCycle>> GetCyclesByStatusAsync(CycleStatus status)
        {
            return await _dbSet
                .Where(tc => tc.Status == status && tc.IsActive)
                .OrderByDescending(tc => tc.CreatedAt)
                .ToListAsync();
        }

        public async Task<TreatmentCycle?> GetCycleWithDetailsAsync(int cycleId)
        {
            return await _dbSet
                .Where(tc => tc.Id == cycleId && tc.IsActive)
                .FirstOrDefaultAsync();
        }

        public async Task<TreatmentCycle> AddTreatmentCycleAsync(TreatmentCycle treatmentCycle)
        {
            await _context.TreatmentCycles.AddAsync(treatmentCycle);
            await _context.SaveChangesAsync();
            return treatmentCycle;
        }

        public async Task<decimal> CalculatePhaseCostAsync(int cycleId)
        {
            var cycle = await _context.TreatmentCycles
                .Include(c => c.TreatmentPhases)
                .FirstOrDefaultAsync(c => c.Id == cycleId);

            if (cycle == null)
                throw new ArgumentException($"Treatment cycle with ID {cycleId} not found.", nameof(cycleId));

            return cycle.TreatmentPhases.Sum(p => p.Cost);
        }


        public async Task<TreatmentCycle> GetCycleByIdAsync(int cycleId)
        {
            var treatmentCycle = await _context.TreatmentCycles
                .FirstOrDefaultAsync(tc => tc.Id == cycleId);

            if (treatmentCycle == null)
            {
                return null;
            }

            return treatmentCycle;
        }


        public async Task<bool> UpdateDoctorAsync(int cycleId, int doctorId)
        {
            var treatmentCycle = await _context.TreatmentCycles.FirstOrDefaultAsync(tc => tc.Id == cycleId);
            if (treatmentCycle == null)
            {
                throw new Exception("Cycle not found.");
            }

            treatmentCycle.DoctorId = doctorId;

            var result = await _context.SaveChangesAsync();
            return result > 0;
        }


        public Task<bool> UpdateStatusAsync(int cycleId, CycleStatus status)
        {
            var cycle = _context.TreatmentCycles.Find(cycleId);
            if (cycle == null)
            {
                throw new Exception("Cycle not found.");
            }
            cycle.Status = status;

            return _context.SaveChangesAsync().ContinueWith(t => t.Result > 0);
        }

        public Task<bool> UpdateTreatmentCycleAsync(TreatmentCycle treatmentCycle)
        {
            _context.TreatmentCycles.Update(treatmentCycle);
            return _context.SaveChangesAsync().ContinueWith(t => t.Result > 0);
        }
    }
}
