using InfertilityTreatment.Data.Context;
using InfertilityTreatment.Data.Repositories.Implementations;
using InfertilityTreatment.Data.Repositories.Interfaces;
using InfertilityTreatment.Entity.DTOs.Common;
using InfertilityTreatment.Entity.Entities;
using InfertilityTreatment.Entity.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace InfertilityTreatment.Repository
{
    public class TestResultRepository : BaseRepository<TestResult>, ITestResultRepository
    {
        private readonly ApplicationDbContext _context;

        public TestResultRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<PaginatedResultDto<TestResult>> GetTestResultsByCycleAsync(int cycleId, PaginationQueryDTO pagination)
        {

            var query = _context.TestResults.Where(tr => tr.CycleId == cycleId && tr.IsActive);
            var totalCount = await query.CountAsync();
            var items = await query.Skip((pagination.PageNumber - 1) * pagination.PageSize).Take(pagination.PageSize).ToListAsync();
            return new PaginatedResultDto<TestResult>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<PaginatedResultDto<TestResult>> GetTestResultsByTypeAsync(int cycleId, TestResultType type, PaginationQueryDTO pagination)
        {
            var query = _context.TestResults.Where(tr => tr.CycleId == cycleId && tr.TestType == type && tr.IsActive);
            var totalCount = await query.CountAsync();
            var items = await query.Skip((pagination.PageNumber - 1) * pagination.PageSize).Take(pagination.PageSize).ToListAsync();
            return new PaginatedResultDto<TestResult>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<PaginatedResultDto<TestResult>> GetTestResultsAsync(int? cycleId, TestResultType? type, DateTime? date, PaginationQueryDTO pagination)
        {
            var query = _context.TestResults.AsQueryable();
            if (cycleId.HasValue)
                query = query.Where(tr => tr.CycleId == cycleId.Value );
            if (type.HasValue)
                query = query.Where(tr => tr.TestType == type.Value );
            if (date.HasValue)
                query = query.Where(tr => tr.TestDate.Date == date.Value.Date );
            query = query.Where(tr => tr.IsActive);
            var totalCount = await query.CountAsync();
            var items = await query.Skip((pagination.PageNumber - 1) * pagination.PageSize).Take(pagination.PageSize).ToListAsync();
            return new PaginatedResultDto<TestResult>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }
    }
}