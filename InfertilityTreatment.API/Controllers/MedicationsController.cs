using InfertilityTreatment.Business.Interfaces;
using InfertilityTreatment.Entity.DTOs.Common;
using InfertilityTreatment.Entity.DTOs.Medications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace InfertilityTreatment.API.Controllers
{
    [Route("api/medications")]
    [ApiController]
    public class MedicationsController : ControllerBase
    {
        private readonly IMedicationService _medicationService;

        public MedicationsController(IMedicationService medicationService)
        {
            _medicationService = medicationService;
        }

        /// <summary>
        /// Get all medications with filtering and pagination
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllMedications([FromQuery] MedicationFilterDto filters, [FromQuery] PaginationQueryDTO pagination)
        {
            try
            {
                var result = await _medicationService.GetAllMedicationsAsync(filters, pagination);
                return Ok(ApiResponseDto<PaginatedResultDto<MedicationDetailDto>>.CreateSuccess(result, "Medications retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseDto<string>.CreateError("An error occurred while retrieving medications."));
            }
        }

        /// <summary>
        /// Get medication by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetMedicationById(int id)
        {
            try
            {
                var medication = await _medicationService.GetMedicationByIdAsync(id);
                if (medication == null)
                    return NotFound(ApiResponseDto<string>.CreateError("Medication not found."));

                return Ok(ApiResponseDto<MedicationDetailDto>.CreateSuccess(medication, "Medication retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseDto<string>.CreateError("An error occurred while retrieving the medication."));
            }
        }

        /// <summary>
        /// Create a new medication
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Doctor,Manager")]
        public async Task<IActionResult> CreateMedication([FromBody] CreateMedicationDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponseDto<string>.CreateError("Validation failed.", errors));
            }

            try
            {
                var medication = await _medicationService.CreateMedicationAsync(dto);
                return CreatedAtAction(nameof(GetMedicationById), new { id = medication.Id },
                    ApiResponseDto<MedicationDetailDto>.CreateSuccess(medication, "Medication created successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseDto<string>.CreateError("An error occurred while creating the medication."));
            }
        }

        /// <summary>
        /// Update an existing medication
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Doctor,Manager")]
        public async Task<IActionResult> UpdateMedication(int id, [FromBody] UpdateMedicationDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponseDto<string>.CreateError("Validation failed.", errors));
            }

            try
            {
                var medication = await _medicationService.UpdateMedicationAsync(id, dto);
                return Ok(ApiResponseDto<MedicationDetailDto>.CreateSuccess(medication, "Medication updated successfully."));
            }
            catch (ArgumentException ex)
            {
                return NotFound(ApiResponseDto<string>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseDto<string>.CreateError("An error occurred while updating the medication."));
            }
        }

        /// <summary>
        /// Delete a medication (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Doctor,Manager")]
        public async Task<IActionResult> DeleteMedication(int id)
        {
            try
            {
                var result = await _medicationService.DeleteMedicationAsync(id);
                if (!result)
                    return NotFound(ApiResponseDto<string>.CreateError("Medication not found."));

                return Ok(ApiResponseDto<string>.CreateSuccess(null, "Medication deleted successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseDto<string>.CreateError("An error occurred while deleting the medication."));
            }
        }

        /// <summary>
        /// Search medications by name, active ingredient, or manufacturer
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchMedications([FromQuery][Required] string query, [FromQuery] PaginationQueryDTO pagination)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(ApiResponseDto<string>.CreateError("Search query is required."));

            try
            {
                var result = await _medicationService.SearchMedicationsAsync(query, pagination);
                return Ok(ApiResponseDto<PaginatedResultDto<MedicationDetailDto>>.CreateSuccess(result, 
                    $"Found {result.TotalCount} medications matching '{query}'."));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseDto<string>.CreateError("An error occurred while searching medications."));
            }
        }
    }
}
