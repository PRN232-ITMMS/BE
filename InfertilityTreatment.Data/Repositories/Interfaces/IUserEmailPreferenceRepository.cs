using InfertilityTreatment.Entity.Entities;
using InfertilityTreatment.Entity.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Data.Repositories.Interfaces
{
    public interface IUserEmailPreferenceRepository : IBaseRepository<UserEmailPreference>
    {
        Task<UserEmailPreference?> GetUserPreferenceAsync(int userId, NotificationType notificationType);
        Task<List<UserEmailPreference>> GetUserAllPreferencesAsync(int userId);
    }
}
