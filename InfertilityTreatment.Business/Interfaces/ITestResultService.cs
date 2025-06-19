using InfertilityTreatment.Entity.DTOs.Common;
using InfertilityTreatment.Entity.DTOs.TestResults;
using InfertilityTreatment.Entity.Enums;
using System;
using System.Threading.Tasks;

namespace InfertilityTreatment.Business.Interfaces
{
    public interface ITestResultService
    {
        Task<TestResultDto> CreateTestResultAsync(CreateTestResultDto dto);
        Task<PaginatedResultDto<TestResultDto>> GetTestResultsByCycleAsync(int cycleId, PaginationQueryDTO pagination);
        Task<TestResultDetailDto?> GetTestResultByIdAsync(int testResultId);
        Task<TestResultDto> UpdateTestResultAsync(int testResultId, UpdateTestResultDto dto);
        Task<PaginatedResultDto<TestResultDto>> GetTestResultsByTypeAsync(int cycleId, TestResultType type, PaginationQueryDTO pagination);
        Task<PaginatedResultDto<TestResultDto>> GetTestResultsAsync(int? cycleId, TestResultType? type, DateTime? date, PaginationQueryDTO pagination);
    }
}
