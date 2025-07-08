using InfertilityTreatment.Entity.DTOs.Common;
using InfertilityTreatment.Entity.Entities;
using InfertilityTreatment.Entity.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InfertilityTreatment.Data.Repositories.Interfaces
{
    public interface ITestResultRepository : IBaseRepository<TestResult>
    {
        Task<PaginatedResultDto<TestResult>> GetTestResultsByCycleAsync(int cycleId, PaginationQueryDTO pagination);
        Task<PaginatedResultDto<TestResult>> GetTestResultsByTypeAsync(int cycleId, TestResultType type, PaginationQueryDTO pagination);
        Task<PaginatedResultDto<TestResult>> GetTestResultsAsync(int? cycleId, TestResultType? type, DateTime? date, PaginationQueryDTO pagination);
        
        // Additional methods for phase management
        Task<List<TestResult>> GetByCycleIdAsync(int cycleId);
    }
}
