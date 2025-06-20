using InfertilityTreatment.Entity.Enums;
using InfertilityTreatment.Entity.Entities;

using AutoMapper;
using System.Collections.Generic;
using System.Threading.Tasks;
using InfertilityTreatment.Business.Interfaces;
using InfertilityTreatment.Data.Repositories.Interfaces;
using InfertilityTreatment.Entity.DTOs.TestResultss;
using InfertilityTreatment.Entity.DTOs.Common;

namespace InfertilityTreatment.Service.Services
{
    public class TestResultService : ITestResultService
    {
        private readonly ITestResultRepository _repo;
        private readonly ITreatmentCycleRepository _cycleRepo;
        private readonly IMapper _mapper;

        public TestResultService(ITestResultRepository repo, ITreatmentCycleRepository cycleRepo, IMapper mapper)
        {
            _repo = repo;
            _cycleRepo = cycleRepo;
            _mapper = mapper;
        }

        public async Task<TestResultDto> CreateTestResultAsync(CreateTestResultDto dto)
        {
            // Business validation: TestDate không được ở tương lai
            if (dto.TestDate > DateTime.UtcNow)
                throw new ArgumentException("Test date cannot be in the future");
            // Check CycleId tồn tại
            var cycle = await _cycleRepo.GetByIdAsync(dto.CycleId);
            if (cycle == null)
                throw new ArgumentException($"Treatment cycle with id {dto.CycleId} does not exist");
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
        private string InterpretResult(TestResult entity)
        {
            if (decimal.TryParse(entity.Results, out var resultValue) &&
                TryParseRange(entity.ReferenceRange, out var min, out var max))
            {
                if (resultValue >= min && resultValue <= max)
                    return "Normal";
                return "Abnormal";
            }

            return "RequiresAttention";
        }

        private bool TryParseRange(string? range, out decimal min, out decimal max)
        {
            min = max = 0;
            if (string.IsNullOrWhiteSpace(range)) return false;

            var parts = range.Split('-', StringSplitOptions.TrimEntries);
            return parts.Length == 2 &&
                   decimal.TryParse(parts[0], out min) &&
                   decimal.TryParse(parts[1], out max);
        }

        public async Task<TestResultDto> UpdateTestResultAsync(int testResultId, UpdateTestResultDto dto)
        {
            var entity = await _repo.GetByIdAsync(testResultId);
            if (entity == null) throw new Exception("TestResult not found");
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