using InfertilityTreatment.Entity.DTOs.Common;
using InfertilityTreatment.Entity.DTOs.Users;
using InfertilityTreatment.Entity.Entities;

namespace InfertilityTreatment.Data.Repositories.Interfaces
{
    public interface IUserRepository : IBaseRepository<User>
    {
        Task<User?> FindByEmailAsync(string email);
        Task<User?> FindByEmailWithRelatedDataAsync(string email);
        Task<User?> GetByIdWithRelatedDataAsync(int id);
        Task<PaginatedResultDto<User?>> GetUsers(UserFilterDto filter);
        Task<User?> UpdateProfile(User user);
        Task<User?> ChangePasswordAsync(int userId, string password);
        Task<bool> EmailExistsAsync(string email);
        Task<User?> GetByIdWithProfilesAsync(int id);
        Task<List<User>> GetUsersByRolesAsync(List<string> roles);
    }
}