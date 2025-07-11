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
using FluentValidation;
using InfertilityTreatment.Data.Repositories.Interfaces;
using OfficeOpenXml.Packaging;

namespace InfertilityTreatment.API.Controllers
{
    [Route("api/treatment-cycles")]
    [Authorize]
    [ApiController]
    public class TreatmentCyclesController : ControllerBase
    {
        private readonly ICycleService _cycleService;
        private readonly IValidator<CreateCycleDto> _createCycleValidator;
        private readonly IValidator<InitializeCycleDto> _initializeCycleValidator;
        private readonly IValidator<StartTreatmentDto> _startTreatmentValidator;
        private readonly ICustomerRepository _customerRepository;
        private readonly IDoctorRepository _doctorRepository;
        
        public TreatmentCyclesController(
            ICycleService cycleService, 
            IValidator<CreateCycleDto> createCycleValidator,
            IValidator<InitializeCycleDto> initializeCycleValidator,
            IValidator<StartTreatmentDto> startTreatmentValidator,
            ICustomerRepository customerRepository,
            IDoctorRepository doctorRepository)
        {
            _cycleService = cycleService;
            _createCycleValidator = createCycleValidator;
            _initializeCycleValidator = initializeCycleValidator;
            _startTreatmentValidator = startTreatmentValidator;
            _customerRepository = customerRepository;
            _doctorRepository = doctorRepository;
        }

        [HttpPost]
        public async Task<IActionResult> CreateCycle([FromBody] CreateCycleDto createCycleDto)
        {
            var validationResult = await _createCycleValidator.ValidateAsync(createCycleDto);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.ToDictionary());

            try
            {
                var cycleResponse = await _cycleService.CreateCycleAsync(createCycleDto);
                return Ok(ApiResponseDto<CycleResponseDto>.CreateSuccess(cycleResponse, "Treatment cycle created successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateError(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateError(ex.Message));
            }
            catch (ApplicationException ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateError(ex.Message));
            }
        }

        [HttpGet]
        [Authorize(Roles = nameof(UserRole.Doctor) + "," + nameof(UserRole.Customer))]
        public async Task<IActionResult> GetTreatmentCycles([FromQuery] TreatmentCycleFilterDto filter)
        {
            if (filter.PageSize > 100)
                filter.PageSize = 100;

            if (filter.PageNumber < 1)
                filter.PageNumber = 1;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return BadRequest(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Invalid user ID",
                    Data = null
                });
            }

            if (string.IsNullOrEmpty(roleClaim))
            {
                return BadRequest(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Invalid user role",
                    Data = null
                });
            }
            Enum.TryParse<UserRole>(roleClaim, true, out UserRole userRole);

            if (userRole == UserRole.Customer)
            {
                var customerResult = await _cycleService.GetCyclesByCustomerAsync(userId, filter);
                return Ok(ApiResponseDto<PaginatedResultDto<CycleResponseDto>>.CreateSuccess(customerResult, "Retrieved cycles by customer successfully.."));
            }
            var doctorResult = await _cycleService.GetCyclesByDoctorAsync(userId, filter);
            return Ok(ApiResponseDto<PaginatedResultDto<CycleResponseDto>>.CreateSuccess(doctorResult, "Retrieved cycles by doctor successfully."));
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetCycleById(int id)
        {
            try
            {
                var cycle = await _cycleService.GetCycleByIdAsync(id);
                if (cycle == null)
                {
                    return NotFound(ApiResponseDto<string>.CreateError("Cycle not found."));
                }

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var roleStr = User.FindFirstValue(ClaimTypes.Role);
                if (!Enum.TryParse<UserRole>(roleStr, out var userRole))
                    return Forbid();

                // Check authorization based on user role and relationships
                var hasAccess = await CheckCycleAccessAsync(cycle, userId, userRole);
                if (!hasAccess)
                {
                    return Forbid();
                }

                return Ok(ApiResponseDto<CycleDetailDto>.CreateSuccess(cycle, "Cycle details retrieved successfully."));
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseDto<string>.CreateError("Error retrieving cycle details."));
            }
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCycle(int id, [FromBody] UpdateCycleDto updateCycleDto)
        {
            try
            {
                var cycle = await _cycleService.GetCycleByIdAsync(id);
                if (cycle == null)
                {
                    return NotFound(ApiResponseDto<string>.CreateError("Cycle not found."));
                }

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var roleStr = User.FindFirstValue(ClaimTypes.Role);
                if (!Enum.TryParse<UserRole>(roleStr, out var userRole))
                    return Forbid();

                var hasAccess = await CheckCycleAccessAsync(cycle, userId, userRole);
                if (!hasAccess)
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
                var result = await _cycleService.GetCyclePhasesAsync(id, filter);
                return Ok(ApiResponseDto<PaginatedResultDto<PhaseResponseDto>>.CreateSuccess(result, "Phases retrieved for cycle."));
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseDto<List<PhaseResponseDto>>.CreateError("Error fetching phases for the cycle."));
            }
        }

        [HttpPost("{id}/phases")]
        public async Task<IActionResult> CreatePhase(int id, [FromBody]CreatePhaseDto createPhaseDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _cycleService.AddPhaseAsync(id, createPhaseDto);
                return Ok(ApiResponseDto<PhaseResponseDto>.CreateSuccess(result, "New phase added to cycle."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateError(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateError(ex.Message));
            }
            catch (ApplicationException ex)
            {
                return StatusCode(500, ApiResponseDto<string>.CreateError(ex.Message));
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

        #region Phase Management Endpoints BE-022

        /// <summary>
        /// Start a specific phase in a treatment cycle
        /// </summary>
        [HttpPatch("{cycleId}/phases/{phaseId}/start")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> StartPhase(int cycleId, int phaseId, [FromBody] StartPhaseDto dto)
        {
            try
            {
                var result = await _cycleService.StartPhaseAsync(cycleId, phaseId, dto);
                return Ok(ApiResponseDto<PhaseResponseDto>.CreateSuccess(result, "Treatment Phase started successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateError(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseDto<string>.CreateError("An error occurred while starting the phase."));
            }
        }

        /// <summary>
        /// Complete a specific phase in a treatment cycle
        /// </summary>
        [HttpPatch("{cycleId}/phases/{phaseId}/complete")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> CompletePhase(int cycleId, int phaseId, [FromBody] CompletePhaseDto dto)
        {
            try
            {
                var result = await _cycleService.CompletePhaseAsync(cycleId, phaseId, dto);
                return Ok(ApiResponseDto<PhaseResponseDto>.CreateSuccess(result, "Treatment Phase completed successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateError(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseDto<string>.CreateError("An error occurred while completing the phase."));
            }
        }

        /// <summary>
        /// Get progress information for a specific phase
        /// </summary>
        [HttpGet("{cycleId}/phases/{phaseId}/progress")]
        [Authorize(Roles = "Doctor,Customer,Admin")]
        public async Task<IActionResult> GetPhaseProgress(int cycleId, int phaseId)
        {
            try
            {
                var result = await _cycleService.GetPhaseProgressAsync(cycleId, phaseId);
                return Ok(ApiResponseDto<PhaseProgressDto>.CreateSuccess(result, "Treatment Phase progress retrieved successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseDto<string>.CreateError("An error occurred while retrieving phase progress."));
            }
        }

        /// <summary>
        /// Generate default phases for a treatment cycle based on treatment type
        /// </summary>
        [HttpPost("{cycleId}/phases/generate")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> GenerateDefaultPhases(int cycleId, [FromBody] GeneratePhasesDto dto)
        {
            try
            {
                var result = await _cycleService.GenerateDefaultPhasesAsync(cycleId, dto);
                return Ok(ApiResponseDto<List<PhaseResponseDto>>.CreateSuccess(result, 
                    $"Generated {result.Count} default phases for {dto.TreatmentType} treatment."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateError(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseDto<string>.CreateError("An error occurred while generating default phases."));
            }
        }

        #endregion

        #region Cycle Initialization Workflow Endpoints

        /// <summary>
        /// Initialize a treatment cycle with treatment plan and phases
        /// </summary>
        [HttpPost("{id}/initialize")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> InitializeCycle(int id, [FromBody] InitializeCycleDto dto)
        {
            var validationResult = await _initializeCycleValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.ToDictionary());

            try
            {
                var result = await _cycleService.InitializeCycleAsync(id, dto);
                return Ok(ApiResponseDto<CycleResponseDto>.CreateSuccess(result, "Treatment cycle initialized successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateError(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseDto<string>.CreateError("An error occurred while initializing the cycle."));
            }
        }

        /// <summary>
        /// Start treatment for an initialized cycle
        /// </summary>
        [HttpPost("{id}/start-treatment")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> StartTreatment(int id, [FromBody] StartTreatmentDto dto)
        {
            var validationResult = await _startTreatmentValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.ToDictionary());

            try
            {
                var result = await _cycleService.StartTreatmentAsync(id, dto);
                return Ok(ApiResponseDto<CycleResponseDto>.CreateSuccess(result, "Treatment started successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateError(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseDto<string>.CreateError("An error occurred while starting treatment."));
            }
        }

        /// <summary>
        /// Get the timeline of events for a treatment cycle
        /// </summary>
        [HttpGet("{id}/timeline")]
        [Authorize(Roles = "Doctor,Customer,Admin")]
        public async Task<IActionResult> GetCycleTimeline(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var roleStr = User.FindFirstValue(ClaimTypes.Role);
                if (!Enum.TryParse<UserRole>(roleStr, out var userRole))
                    return Forbid();

                // Check if user has access to this cycle
                var cycle = await _cycleService.GetCycleByIdAsync(id);
                if (cycle == null)
                    return NotFound(ApiResponseDto<string>.CreateError("Cycle not found."));

                var hasAccess = await CheckCycleAccessAsync(cycle, userId, userRole);
                if (!hasAccess)
                    return Forbid();

                var result = await _cycleService.GetCycleTimelineAsync(id);
                return Ok(ApiResponseDto<CycleTimelineDto>.CreateSuccess(result, "Cycle timeline retrieved successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseDto<string>.CreateError("An error occurred while retrieving cycle timeline."));
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Check if current user has access to the treatment cycle
        /// </summary>
        private async Task<bool> CheckCycleAccessAsync(CycleDetailDto cycle, int userId, UserRole userRole)
        {
            switch (userRole)
            {
                case UserRole.Admin:
                    return true; // Admin has access to all cycles

                case UserRole.Customer:
                    // Get customer by userId and check if it matches cycle.CustomerId
                    var customer = await _customerRepository.GetByUserIdAsync(userId);
                    return customer != null && customer.Id == cycle.CustomerId;

                case UserRole.Doctor:
                    // Get doctor by userId and check if it matches cycle.DoctorId  
                    var doctor = await _doctorRepository.GetByUserIdAsync(userId);
                    return doctor != null && doctor.Id == cycle.DoctorId;

                default:
                    return false;
            }
        }

        #endregion
    }
}
