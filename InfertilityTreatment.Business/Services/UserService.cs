using AutoMapper;
using InfertilityTreatment.Business.Helpers;
using InfertilityTreatment.Business.Interfaces;
using InfertilityTreatment.Data.Repositories.Interfaces;
using InfertilityTreatment.Entity.DTOs.Common;
using InfertilityTreatment.Entity.DTOs.Users;
using InfertilityTreatment.Entity.Entities;
using InfertilityTreatment.Entity.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Business.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public UserService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<string> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId)
       ?? throw new KeyNotFoundException("User not found.");
            var isCurrentPasswordCorrect = PasswordHelper.VerifyPassword(changePasswordDto.CurrentPassword, user.PasswordHash);
            if (!isCurrentPasswordCorrect)
                throw new ArgumentException("Current password is incorrect.");

            var isNewPasswordSameAsOld = PasswordHelper.VerifyPassword(changePasswordDto.NewPassword, user.PasswordHash);
            if (isNewPasswordSameAsOld)
                throw new ArgumentException("New password must be different from the current password.");


            var password = PasswordHelper.HashPassword(changePasswordDto.NewPassword);

            var result = await _unitOfWork.Users.ChangePasswordAsync(userId, password);
            if (result == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }
            return "Profile updated successfully";

        }

        public async Task<UserProfileDto> GetProfileAsync(int userId)
        {
            var user = await _unitOfWork.Users.GetByIdWithProfilesAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }
            var profileDto = _mapper.Map<UserProfileDto>(user);
            return profileDto;
        }

        public async Task<PaginatedResultDto<UserProfileDto>> GetUsersAsync(UserFilterDto filter)
        {
            var pagedResult = await _unitOfWork.Users.GetUsers(filter);

            if (pagedResult == null || !pagedResult.Items.Any())
            {
                throw new KeyNotFoundException("No users found.");
            }

            var profileDtos = _mapper.Map<List<UserProfileDto>>(pagedResult.Items);

            return new PaginatedResultDto<UserProfileDto>(
                profileDtos,
                pagedResult.TotalCount,
                pagedResult.PageNumber,
                pagedResult.PageSize
            );
        }

        public async Task<string> UpdateProfileAsync(int userId, UpdateProfileDto updateProfileDto)
        {
            var user = _mapper.Map<User>(updateProfileDto);
            user.Id = userId;

            var result = await _unitOfWork.Users.UpdateProfile(user);
            if (result == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }
            return "Profile updated successfully";
        }

        public async Task<UserProfileDto> CreateUserAsync(CreateUserDto createUserDto)
        {
            try
            {
                // Begin transaction
                await _unitOfWork.BeginTransactionAsync();

                // Validate role - only allow Doctor or Manager
                if (createUserDto.Role != UserRole.Doctor && createUserDto.Role != UserRole.Manager)
                {
                    throw new ArgumentException("Only Doctor or Manager roles are allowed for this endpoint");
                }

                // Check if email already exists
                if (await _unitOfWork.Users.EmailExistsAsync(createUserDto.Email))
                {
                    throw new ArgumentException("Email is already registered");
                }

                // Validate doctor-specific fields if role is Doctor
                if (createUserDto.Role == UserRole.Doctor)
                {
                    if (string.IsNullOrWhiteSpace(createUserDto.LicenseNumber))
                    {
                        throw new ArgumentException("License number is required for Doctor role");
                    }
                    if (string.IsNullOrWhiteSpace(createUserDto.Specialization))
                    {
                        throw new ArgumentException("Specialization is required for Doctor role");
                    }
                    if (!createUserDto.YearsOfExperience.HasValue || createUserDto.YearsOfExperience < 0)
                    {
                        throw new ArgumentException("Years of experience is required for Doctor role");
                    }
                }

                // Create user entity
                var user = _mapper.Map<User>(createUserDto);
                user.PasswordHash = PasswordHelper.HashPassword(createUserDto.Password);
                user.CreatedAt = DateTime.UtcNow;
                user.IsActive = true;

                // Add user to database
                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();

                // If role is Doctor, create doctor profile
                if (createUserDto.Role == UserRole.Doctor)
                {
                    var doctor = new Doctor
                    {
                        UserId = user.Id,
                        LicenseNumber = createUserDto.LicenseNumber!,
                        Specialization = createUserDto.Specialization,
                        YearsOfExperience = createUserDto.YearsOfExperience!.Value,
                        Education = createUserDto.Education,
                        Biography = createUserDto.Biography,
                        ConsultationFee = createUserDto.ConsultationFee,
                        SuccessRate = createUserDto.SuccessRate,
                        IsAvailable = true,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    await _unitOfWork.Doctors.AddDoctorAsync(doctor);
                    await _unitOfWork.SaveChangesAsync();
                }

                // Commit transaction
                await _unitOfWork.CommitTransactionAsync();

                // Return the created user profile
                var createdUser = await _unitOfWork.Users.GetByIdWithProfilesAsync(user.Id);
                return _mapper.Map<UserProfileDto>(createdUser);
            }
            catch (Exception)
            {
                // Rollback transaction
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

    }

}
