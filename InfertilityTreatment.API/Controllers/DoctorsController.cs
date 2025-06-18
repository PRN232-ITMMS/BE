using InfertilityTreatment.Business.Interfaces;
using InfertilityTreatment.Entity.DTOs.Common;
using InfertilityTreatment.Entity.DTOs.Doctors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace InfertilityTreatment.API.Controllers
{
    [Route("api/doctors/")]
    [ApiController]
    public class DoctorsController : ControllerBase
    {
        private readonly IDoctorService _doctorService;

        public DoctorsController(IDoctorService doctorService)
        {
            _doctorService = doctorService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDoctors([FromQuery] DoctorFilterDto filter)
        {
            if (filter.PageSize > 100)
                filter.PageSize = 100;

            if (filter.PageNumber < 1)
                filter.PageNumber = 1;

            var result = await _doctorService.GetAllDoctorsAsync(filter);
            return Ok(ApiResponseDto<PaginatedResultDto<DoctorResponseDto>>.CreateSuccess(result));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDoctorById(int id)
        {
            var doctor = await _doctorService.GetDoctorByIdAsync(id);
            if (doctor == null)
                return NotFound(ApiResponseDto<DoctorDetailDto>.CreateError("Doctor not found"));
            return Ok(ApiResponseDto<DoctorDetailDto>.CreateSuccess(doctor));
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateDoctorProfile(int id, [FromBody] UpdateDoctorDto updateDoctorDto)
        {
            var updated = await _doctorService.UpdateDoctorProfileAsync(id, updateDoctorDto);
            if (updated == null)
                return NotFound(ApiResponseDto<DoctorDetailDto>.CreateError("Doctor not found"));
            return Ok(ApiResponseDto<DoctorDetailDto>.CreateSuccess(updated, "Doctor updated successfully"));
        }

        [HttpPut("{id}/availability")]
        public async Task<IActionResult> ToggleAvailability(int id)
        {
            var updated = await _doctorService.ToggleAvailabilityAsync(id);
            if (updated == null)
                return NotFound(ApiResponseDto<DoctorDetailDto>.CreateError("Doctor not found"));
            return Ok(ApiResponseDto<DoctorDetailDto>.CreateSuccess(updated, "Availability toggled successfully"));
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchDoctors([FromQuery] DoctorSearchDto searchDto)
        {
            var doctors = await _doctorService.SearchDoctorsAsync(searchDto);
            return Ok(ApiResponseDto<List<DoctorResponseDto>>.CreateSuccess(doctors));
        }

        //[HttpPost]
        //public async Task<IActionResult> CreateDoctor([FromBody] CreateDoctorDto createDoctorDto)
        //{
        //    var doctor = await _doctorService.CreateDoctorAsync(createDoctorDto);
        //    return CreatedAtAction(nameof(GetDoctorById), new { id = doctor.Id }, ApiResponseDto<DoctorDetailDto>.CreateSuccess(doctor, "Doctor created successfully"));
        //}
    }
}
