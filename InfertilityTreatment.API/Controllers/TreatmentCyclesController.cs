using InfertilityTreatment.Business.Interfaces;
using InfertilityTreatment.Entity.Constants;
using InfertilityTreatment.Entity.DTOs.Common;
using InfertilityTreatment.Entity.DTOs.TreatmentCycles;
using InfertilityTreatment.Entity.DTOs.TreatmentPhase;
using InfertilityTreatment.Entity.DTOs.Users;
using InfertilityTreatment.Entity.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InfertilityTreatment.API.Controllers
{
    [Route("api/treatment-cycles")]
    [Authorize]
    [ApiController]
    public class TreatmentCyclesController : ControllerBase
    {
        private readonly ICycleService _cycleService;
        public TreatmentCyclesController(ICycleService cycleService)
        {
            _cycleService = cycleService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateCycle([FromBody] CreateCycleDto createCycleDto)
        {
            var cycleResponse = await _cycleService.CreateCycleAsync(createCycleDto);
            if (cycleResponse == null)
                return BadRequest("Failed to create treatment cycle. Please verify input data.");

            return Ok(ApiResponseDto<CycleResponseDto>.CreateSuccess(cycleResponse, "Treatment cycle created successfully."));
        }

        [HttpGet]
        [Authorize(Roles = nameof(UserRole.Doctor) + "," + nameof(UserRole.Manager))]
        public async Task<IActionResult> GetTreatmentCycles([FromQuery] TreatmentCycleFilterDto filter)
        {
            if (filter.PageSize > 100)
                filter.PageSize = 100;

            if (filter.PageNumber < 1)
                filter.PageNumber = 1;

            if (filter.CustomerId.HasValue)
            {
                var result = await _cycleService.GetCyclesByCustomerAsync(filter.CustomerId.Value, filter);
                return Ok(ApiResponseDto<PaginatedResultDto<CycleResponseDto>>.CreateSuccess(result, "Retrieved cycles by customer successfully."));
            }

            if (filter.DoctorId.HasValue)
            {
                var result = await _cycleService.GetCyclesByDoctorAsync(filter.DoctorId.Value, filter);
                return Ok(ApiResponseDto<PaginatedResultDto<CycleResponseDto>>.CreateSuccess(result, "Retrieved cycles by doctor successfully."));
            }

            return BadRequest("Please provide either a valid customerId or doctorId.");
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetCycleById(int id)
        {
            try
            {
                var cycle = await _cycleService.GetCycleByIdAsync(id);
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (cycle.CustomerId != userId && !User.IsInRole("Admin") && cycle.DoctorId != userId)
                {
                    return Forbid(); 
                }
                return Ok(ApiResponseDto<CycleDetailDto>.CreateSuccess(cycle, "Cycle details retrieved successfully."));
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseDto<string>.CreateError("Error retrieving cycle details."));
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCycle(int id, [FromBody] UpdateCycleDto updateCycleDto)
        {
            try
            {
                var cycle = await _cycleService.GetCycleByIdAsync(id);
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (cycle.CustomerId != userId && cycle.DoctorId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }
                var result = await _cycleService.UpdateCycleAsync(id, updateCycleDto);
                if (result)
                    return Ok(ApiResponseDto<string>.CreateSuccess(null, "Cycle updated successfully."));

                return NotFound(ApiResponseDto<string>.CreateError("Cycle not found or could not be updated."));
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseDto<string>.CreateError("Unexpected error during cycle update."));
            }
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateCycleStatus(int id, CycleStatus cycleStatus)
        {
            try
            {
                var result = await _cycleService.UpdateCycleStatusAsync(id, cycleStatus);
                return Ok(ApiResponseDto<string>.CreateSuccess(null, "Cycle status updated successfully."));
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseDto<string>.CreateError("Error updating cycle status."));
            }
        }

        [HttpPut("{id}/assign-doctor")]
        public async Task<IActionResult> AssignDoctorToCycle(int id, [FromBody] AssignDoctorDto assignDoctorDto)
        {
            try
            {
                var result = await _cycleService.AssignDoctorToCycleAsync(id, assignDoctorDto.DoctorId);
                if (result)
                    return Ok(ApiResponseDto<string>.CreateSuccess(null, "Doctor assigned to cycle successfully."));

                return BadRequest(ApiResponseDto<string>.CreateError("Unable to assign doctor to the specified cycle."));
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseDto<string>.CreateError("Error occurred during doctor assignment."));
            }
        }

        [HttpGet("{id}/cost")]
        public async Task<IActionResult> CalculateCycleCost(int id)
        {
            try
            {
                var cost = await _cycleService.CalculateCycleCostAsync(id);
                return Ok(ApiResponseDto<decimal>.CreateSuccess(cost, "Cycle cost calculated."));
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseDto<decimal>.CreateError("Failed to calculate cycle cost."));
            }
        }

        [HttpGet("{id}/phases")]
        public async Task<IActionResult> GetPhasesByCycleId(int id, [FromQuery] TreatmentPhaseFilterDto filter)
        {
            try
            {
                if (filter.PageSize > 100)
                    filter.PageSize = 100;

                if (filter.PageNumber < 1)
                    filter.PageNumber = 1;
                var result = await _cycleService.GetCyclePhasesAsync(id,filter);
                return Ok(ApiResponseDto<PaginatedResultDto<PhaseResponseDto>>.CreateSuccess(result, "Phases retrieved for cycle."));
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseDto<List<PhaseResponseDto>>.CreateError("Error fetching phases for the cycle."));
            }
        }

        [HttpPost("{id}/phases")]
        public async Task<IActionResult> CreatePhase(int id, CreatePhaseDto createPhaseDto)
        {
            try
            {
                var result = await _cycleService.AddPhaseAsync(id, createPhaseDto);
                return Ok(ApiResponseDto<PhaseResponseDto>.CreateSuccess(result, "New phase added to cycle."));
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseDto<PhaseResponseDto>.CreateError("Error creating new phase."));
            }
        }

        [HttpPut("phases/{phaseId}")]
        public async Task<IActionResult> UpdatePhase(int phaseId, UpdatePhaseDto updatePhaseDto)
        {
            try
            {
                var result = await _cycleService.UpdatePhaseAsync(phaseId, updatePhaseDto);
                if (result)
                    return Ok(ApiResponseDto<string>.CreateSuccess("Phase updated successfully.", null));

                return NotFound(ApiResponseDto<string>.CreateError("Phase not found or update failed."));
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseDto<string>.CreateError("An error occurred while updating the phase."));
            }
        }
    }
}
