using InfertilityTreatment.Entity.Enums;
using InfertilityTreatment.Entity.Entities;

using AutoMapper;
using System.Collections.Generic;
using System.Threading.Tasks;
using InfertilityTreatment.Business.Interfaces;
using InfertilityTreatment.Data.Repositories.Interfaces;
using InfertilityTreatment.Entity.DTOs.TestResults;
using InfertilityTreatment.Entity.DTOs.Common;

namespace InfertilityTreatment.Service.Services
{
    public class TestResultService : ITestResultService
    {
        private readonly ITestResultRepository _repo;
        private readonly IMapper _mapper;

        public TestResultService(ITestResultRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<TestResultDto> CreateTestResultAsync(CreateTestResultDto dto)
        {
            var entity = _mapper.Map<TestResult>(dto);
            var created = await _repo.AddAsync(entity);
            await _repo.SaveChangesAsync();
            return _mapper.Map<TestResultDto>(created);
        }

        public async Task<PaginatedResultDto<TestResultDto>> GetTestResultsByCycleAsync(int cycleId, PaginationQueryDTO pagination)
        {
            var paged = await _repo.GetTestResultsByCycleAsync(cycleId, pagination);
            return new PaginatedResultDto<TestResultDto>(
                _mapper.Map<List<TestResultDto>>(paged.Items),
                paged.TotalCount,
                paged.PageNumber,
                paged.PageSize
            );
        }

        public async Task<TestResultDetailDto?> GetTestResultByIdAsync(int testResultId)
        {
            var entity = await _repo.GetByIdAsync(testResultId);
            if (entity == null) return null;
            var detailDto = _mapper.Map<TestResultDetailDto>(entity);
            detailDto.ResultInterpretation = InterpretResult(entity);
            return detailDto;
        }

        // Helper for result interpretation logic
        private string InterpretResult(Entity.Entities.TestResult entity)
        {
            // TODO: Implement real logic based on TestType, Results, ReferenceRange
            if (string.IsNullOrEmpty(entity.Results) || string.IsNullOrEmpty(entity.ReferenceRange))
                return "RequiresAttention";
            // Example: simple numeric comparison for demonstration
            if (decimal.TryParse(entity.Results, out var resultValue) && decimal.TryParse(entity.ReferenceRange, out var refValue))
            {
                if (resultValue == refValue) return "Normal";
                if (resultValue > refValue) return "Abnormal";
            }
            return "RequiresAttention";
        }

        public async Task<TestResultDto> UpdateTestResultAsync(int testResultId, UpdateTestResultDto dto)
        {
            var entity = await _repo.GetByIdAsync(testResultId);
            if (entity == null) throw new KeyNotFoundException("TestResult not found");
            _mapper.Map(dto, entity);
            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();
            return _mapper.Map<TestResultDto>(entity);
        }

        public async Task<PaginatedResultDto<TestResultDto>> GetTestResultsByTypeAsync(int cycleId, TestResultType type, PaginationQueryDTO pagination)
        {
            var paged = await _repo.GetTestResultsByTypeAsync(cycleId, type, pagination);
            return new PaginatedResultDto<TestResultDto>(
                _mapper.Map<List<TestResultDto>>(paged.Items),
                paged.TotalCount,
                paged.PageNumber,
                paged.PageSize
            );
        }

        public async Task<PaginatedResultDto<TestResultDto>> GetTestResultsAsync(int? cycleId, TestResultType? type, DateTime? date, PaginationQueryDTO pagination)
        {
            var paged = await _repo.GetTestResultsAsync(cycleId, type, date, pagination);
            return new PaginatedResultDto<TestResultDto>(
                _mapper.Map<List<TestResultDto>>(paged.Items),
                paged.TotalCount,
                paged.PageNumber,
                paged.PageSize
            );
        }
    }
}