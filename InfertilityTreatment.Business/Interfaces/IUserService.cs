using InfertilityTreatment.Entity.DTOs.Common;
using InfertilityTreatment.Entity.DTOs.Users;
using InfertilityTreatment.Entity.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Business.Interfaces
{
    public interface IUserService
    {
        Task<UserProfileDto> GetProfileAsync(int userId);
        Task<PaginatedResultDto<UserProfileDto>> GetUsersAsync(UserFilterDto filter);
        Task<string> UpdateProfileAsync(int userId, UpdateProfileDto updateProfileDto);
        Task<string> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto);
        Task<UserProfileDto> CreateUserAsync(CreateUserDto createUserDto);
    }
}
