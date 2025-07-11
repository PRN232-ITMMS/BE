using Microsoft.EntityFrameworkCore;
using InfertilityTreatment.Data.Context;
using InfertilityTreatment.Business.Interfaces;
using InfertilityTreatment.Entity.DTOs.Common;

namespace InfertilityTreatment.Business.Services
{
    public class QueryOptimizationService : IQueryOptimizationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _auditLogService;

        public QueryOptimizationService(ApplicationDbContext context, IAuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        public async Task<T> ExecuteOptimizedQueryAsync<T>(Func<Task<T>> query, string operationName)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                // Execute query with query splitting to avoid cartesian products
                var result = await query();
                
                stopwatch.Stop();
                await _auditLogService.LogPerformanceAsync(operationName, stopwatch.ElapsedMilliseconds, true);
                
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await _auditLogService.LogPerformanceAsync(operationName, stopwatch.ElapsedMilliseconds, false, ex.Message);
                throw;
            }
        }

        public IQueryable<T> ApplyOptimizations<T>(IQueryable<T> query) where T : class
        {
            return query
                .AsSplitQuery() // Avoid cartesian products
                .AsNoTracking(); // Read-only queries
        }

        public async Task<PaginatedResultDto<T>> GetPaginatedResultAsync<T>(
            IQueryable<T> query, 
            int pageNumber, 
            int pageSize, 
            string operationName) where T : class
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                // Optimize pagination queries
                var totalCount = await query.CountAsync();
                
                var items = await query
                    .AsSplitQuery()
                    .AsNoTracking()
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                stopwatch.Stop();
                await _auditLogService.LogPerformanceAsync(
                    $"{operationName}_Pagination", 
                    stopwatch.ElapsedMilliseconds, 
                    true, 
                    $"Page {pageNumber}, Size {pageSize}, Total {totalCount}");

                return new PaginatedResultDto<T>(items, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await _auditLogService.LogPerformanceAsync(operationName, stopwatch.ElapsedMilliseconds, false, ex.Message);
                throw;
            }
        }

        public IQueryable<T> IncludeOptimized<T>(IQueryable<T> query, params string[] includes) where T : class
        {
            // Apply includes with split query to avoid N+1 and cartesian products
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
            
            return query.AsSplitQuery();
        }

        public async Task WarmupCriticalQueries()
        {
            // Warmup frequently used queries to improve performance
            try
            {
                // Warmup user count
                _ = await _context.Users.CountAsync();
                
                // Warmup active treatment cycles
                _ = await _context.TreatmentCycles
                    .Where(tc => tc.Status == Entity.Enums.CycleStatus.InProgress)
                    .CountAsync();
                
                // Warmup upcoming appointments
                _ = await _context.Appointments
                    .Where(a => a.ScheduledDateTime >= DateTime.Today)
                    .CountAsync();

                await _auditLogService.LogSystemEventAsync("QueryWarmup", "Critical queries warmed up successfully");
            }
            catch (Exception ex)
            {
                await _auditLogService.LogErrorAsync("QueryOptimizationService", "Failed to warmup queries", ex);
            }
        }

        public async Task<Dictionary<string, object>> GetQueryStatistics()
        {
            var stats = new Dictionary<string, object>();
            
            try
            {
                // Database statistics
                stats["TotalUsers"] = await _context.Users.CountAsync();
                stats["ActiveTreatmentCycles"] = await _context.TreatmentCycles
                    .Where(tc => tc.Status == Entity.Enums.CycleStatus.InProgress)
                    .CountAsync();
             
                stats["PendingAppointments"] = await _context.Appointments
                    .Where(a => a.ScheduledDateTime >= DateTime.Today && 
                               a.Status == Entity.Enums.AppointmentStatus.Scheduled)
                    .CountAsync();

                // Connection pool statistics (if available)
                stats["DatabaseName"] = _context.Database.GetDbConnection().Database;
                stats["ConnectionState"] = _context.Database.GetDbConnection().State.ToString();
                
                return stats;
            }
            catch (Exception ex)
            {
                await _auditLogService.LogErrorAsync("QueryOptimizationService", "Failed to get query statistics", ex);
                return new Dictionary<string, object> { ["Error"] = ex.Message };
            }
        }
    }
}
