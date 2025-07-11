using InfertilityTreatment.Data.Context;
using InfertilityTreatment.Data.Repositories.Interfaces;
using InfertilityTreatment.Entity.DTOs.Common;
using InfertilityTreatment.Entity.DTOs.Users;
using InfertilityTreatment.Entity.Entities;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq.Expressions;

namespace InfertilityTreatment.Data.Repositories.Implementations
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<User?> FindByEmailAsync(string email)
        {
            //return await _dbSet
            //    .Include(u => u.Customer)
            //    .Include(u => u.Doctor)
            //    .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

            var users = await FindWithIncludeAsync(u => u.Email == email,u => u.Customer,u => u.Doctor);
            return users.FirstOrDefault();
        }

        public async Task<User?> FindByEmailWithRelatedDataAsync(string email)
        {
            return await _dbSet
                .Include(u => u.Customer)
                .Include(u => u.Doctor)
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
        }

        public async Task<User?> GetByIdWithRelatedDataAsync(int id)
        {
            return await _dbSet
                .Include(u => u.Customer)
                .Include(u => u.Doctor)
                .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            //return await _dbSet.AnyAsync(u => u.Email == email && u.IsActive);
            var users = await FindWithIncludeAsync(u => u.Email == email);
            return users.Any();
        }

        public async Task<User?> GetByIdWithProfilesAsync(int id)
        {
            return await _dbSet
                .Include(u => u.Customer)
                .Include(u => u.Doctor)
                .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
        }

        public async Task<User?> UpdateProfile(User updatedUser)
        {

            var existingUser = await _dbSet.FirstOrDefaultAsync(u => u.Id == updatedUser.Id && u.IsActive);
            if (existingUser == null)
            {
                throw new KeyNotFoundException($"Error: {updatedUser.Id}.");
            }
            existingUser.FullName = updatedUser.FullName;
            existingUser.Email = updatedUser.Email;
            existingUser.PhoneNumber = updatedUser.PhoneNumber;
            existingUser.Gender = updatedUser.Gender;

            existingUser.UpdatedAt = DateTime.UtcNow;

            _context.Entry(existingUser).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return existingUser;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error: {ex.Message}");
            }
        }

        public async Task<User?> ChangePasswordAsync(int userId, string password)
        {
            var existingUser = await _dbSet.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (existingUser == null)
            {
                throw new KeyNotFoundException($"Error {userId}.");
            }
            existingUser.PasswordHash = password;

            existingUser.UpdatedAt = DateTime.UtcNow;

            _context.Entry(existingUser).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return existingUser;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error: {ex.Message}");
            }
        }

        public async Task<PaginatedResultDto<User?>> GetUsers(UserFilterDto filter)
        {
            var query = _context.Users.AsQueryable();

            var totalCount = await query.CountAsync();

            var pagedUsers = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PaginatedResultDto<User?>(pagedUsers, totalCount, filter.PageNumber, filter.PageSize);
        }

        public async Task<List<User>> GetUsersByRolesAsync(List<string> roles)
        {
            return await _context.Users
                .Where(u => roles.Contains(u.Role.ToString()))
                .ToListAsync();
        }
    }
}
