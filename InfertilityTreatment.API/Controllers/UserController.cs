using InfertilityTreatment.Business.Interfaces;
using InfertilityTreatment.Entity.DTOs.Auth;
using InfertilityTreatment.Entity.DTOs.Common;
using InfertilityTreatment.Entity.DTOs.Doctors;
using InfertilityTreatment.Entity.DTOs.Users;
using InfertilityTreatment.Entity.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FluentValidation;

namespace InfertilityTreatment.API.Controllers
{
    [Route("api/users")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IValidator<CreateUserDto> _createUserValidator;
        
        public UserController(IUserService userService, IValidator<CreateUserDto> createUserValidator)
        {
            _userService = userService;
            _createUserValidator = createUserValidator;
        }
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<ApiResponseDto<PaginatedResultDto<UserProfileDto>>>> GetUsers([FromQuery] UserFilterDto filter)
        {
            try
            {
                if (filter.PageSize > 100)
                    filter.PageSize = 100;

                if (filter.PageNumber < 1)
                    filter.PageNumber = 1;
                var users = await _userService.GetUsersAsync(filter);
                return Ok(ApiResponseDto<PaginatedResultDto<UserProfileDto>>.CreateSuccess(users, "Successfully retrieved user data."));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseDto<string>.CreateError("An error occurred while retrieving list customer."));
            }
        }
        [HttpGet("profile")]
        public async Task<ActionResult<ApiResponseDto<UserProfileDto>>> GetProfile()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _userService.GetProfileAsync(int.Parse(userId));
                return Ok(ApiResponseDto<UserProfileDto>.CreateSuccess(user, "Profile retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseDto<string>.CreateError("An error occurred while retrieving the profile."));
            }   
        }
        [HttpPut("profile")]
        public async Task<ActionResult<string>> UpdateProfile([FromBody]UpdateProfileDto updateProfileDto)
        {
            try {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var updatedProfile = await _userService.UpdateProfileAsync(int.Parse(userId), updateProfileDto);
                return Ok(ApiResponseDto<string>.CreateSuccess(null, updatedProfile));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseDto<string>.CreateError("An error occurred while updating the profile."));
            }

        }
        [HttpPut("change-password")]
        public async Task<ActionResult<string>> ChangePassword(ChangePasswordDto changePasswordDto)
        {
            try {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var result = await _userService.ChangePasswordAsync(int.Parse(userId), changePasswordDto);
                return Ok(ApiResponseDto<string>.CreateSuccess(null, result));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseDto<string>.CreateError("An error occurred while changing the password."));
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<ApiResponseDto<UserProfileDto>>> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            try
            {
                // Validate request
                var validationResult = await _createUserValidator.ValidateAsync(createUserDto);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return BadRequest(ApiResponseDto<UserProfileDto>.CreateError("Validation failed", errors));
                }

                var createdUser = await _userService.CreateUserAsync(createUserDto);
                return Ok(ApiResponseDto<UserProfileDto>.CreateSuccess(createdUser, "User created successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseDto<string>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    ApiResponseDto<string>.CreateError("An error occurred while creating the user."));
            }
        }
    }
}
