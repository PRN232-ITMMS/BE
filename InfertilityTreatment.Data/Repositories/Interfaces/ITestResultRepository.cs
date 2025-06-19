using InfertilityTreatment.Entity.DTOs.Common;
using InfertilityTreatment.Entity.Entities;
using InfertilityTreatment.Entity.Enums;
using System;
using System.Threading.Tasks;

namespace InfertilityTreatment.Data.Repositories.Interfaces
{
    public interface ITestResultRepository : IBaseRepository<TestResult>
    {
        Task<TestResult?> GetByIdAsync(int id);
        Task<PaginatedResultDto<TestResult>> GetTestResultsByCycleAsync(int cycleId, PaginationQueryDTO pagination);
        Task<PaginatedResultDto<TestResult>> GetTestResultsByTypeAsync(int cycleId, TestResultType type, PaginationQueryDTO pagination);
        Task<PaginatedResultDto<TestResult>> GetTestResultsAsync(int? cycleId, TestResultType? type, DateTime? date, PaginationQueryDTO pagination);
    }
}
