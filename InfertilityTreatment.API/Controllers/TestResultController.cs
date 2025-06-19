using InfertilityTreatment.Business.Interfaces;
using InfertilityTreatment.Entity.DTOs.Common;
using InfertilityTreatment.Entity.DTOs.TestResults;
using InfertilityTreatment.Entity.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
namespace YourNamespace.Controllers
{
    [Route("api/test-results")]
    [ApiController]
    public class TestResultController : ControllerBase
    {
        private readonly ITestResultService _service;

        public TestResultController(ITestResultService service)
        {
            _service = service;
        }

        [HttpPost]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> Create([FromBody] CreateTestResultDto dto)
        {
            var result = await _service.CreateTestResultAsync(dto);
            return Ok(ApiResponseDto<TestResultDto>.CreateSuccess(result, "Test result created successfully."));
        }

        [HttpGet]
        public async Task<IActionResult> GetTestResults(
            [FromQuery] int? cycleId,
            [FromQuery] TestResultType? type,
            [FromQuery] DateTime? date,
            [FromQuery] PaginationQueryDTO pagination)
        {
            pagination.PageNumber = pagination.PageNumber <= 0 ? 1 : pagination.PageNumber;
            pagination.PageSize = pagination.PageSize <= 0 ? 100 : pagination.PageSize;
            var result = await _service.GetTestResultsAsync(cycleId, type, date, pagination);
            return Ok(ApiResponseDto<PaginatedResultDto<TestResultDto>>.CreateSuccess(result, "Test results retrieved successfully."));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetTestResultByIdAsync(id);
            if (result == null) return NotFound(ApiResponseDto<TestResultDetailDto>.CreateError("Test result not found."));
            return Ok(ApiResponseDto<TestResultDetailDto>.CreateSuccess(result));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTestResultDto dto)
        {
            var result = await _service.UpdateTestResultAsync(id, dto);
            return Ok(ApiResponseDto<TestResultDto>.CreateSuccess(result, "Test result updated successfully."));
        }

        [HttpGet("/api/treatment-cycles/{cycleId}/test-results")]
        public async Task<IActionResult> GetByCycleId(int cycleId, [FromQuery] PaginationQueryDTO pagination)
        {
            pagination.PageNumber = pagination.PageNumber <= 0 ? 1 : pagination.PageNumber;
            pagination.PageSize = pagination.PageSize <= 0 ? 100 : pagination.PageSize;
            var result = await _service.GetTestResultsByCycleAsync(cycleId, pagination);
            return Ok(ApiResponseDto<PaginatedResultDto<TestResultDto>>.CreateSuccess(result, "Test results retrieved successfully."));
        }
    }
}