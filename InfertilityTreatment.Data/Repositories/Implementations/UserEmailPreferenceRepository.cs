using InfertilityTreatment.Data.Context;
using InfertilityTreatment.Data.Repositories.Interfaces;
using InfertilityTreatment.Entity.Entities;
using InfertilityTreatment.Entity.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Data.Repositories.Implementations
{
    public class UserEmailPreferenceRepository : BaseRepository<UserEmailPreference>, IUserEmailPreferenceRepository
    {
        public UserEmailPreferenceRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<UserEmailPreference?> GetUserPreferenceAsync(int userId, NotificationType notificationType)
        {
            return await _dbSet.FirstOrDefaultAsync(p => p.UserId == userId && p.NotificationType == notificationType && p.IsActive);
        }

        public async Task<List<UserEmailPreference>> GetUserAllPreferencesAsync(int userId)
        {
            return await _dbSet.Where(p => p.UserId == userId && p.IsActive).ToListAsync();
        }
    }
}
