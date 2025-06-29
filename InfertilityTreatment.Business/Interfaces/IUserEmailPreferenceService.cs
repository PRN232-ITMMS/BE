using InfertilityTreatment.Entity.DTOs.Emails;
using InfertilityTreatment.Entity.Entities;
using InfertilityTreatment.Entity.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Business.Interfaces
{
    public interface IUserEmailPreferenceService
    {
        Task<UserEmailPreference?> GetUserPreferenceAsync(int userId, NotificationType notificationType);
        Task<List<UserEmailPreference>> GetUserAllPreferencesAsync(int userId);
        Task<bool> UpdateUserPreferenceAsync(UpdateUserEmailPreferenceDto updateDto);
        Task<UserEmailPreferenceResponseDto?> CreateUserEmailPreferenceAsync(CreateUserEmailPreferenceDto createDto);
        Task<UserEmailPreferenceResponseDto?> GetUserEmailPreferenceByIdAsync(int preferenceId, int userId);
        Task<bool> DeleteUserEmailPreferenceAsync(int preferenceId, int userId);
        Task<bool> IsEmailNotificationEnabledAsync(int userId, NotificationType notificationType);
        Task<UserEmailPreference?> GetUserEmailPreferenceIfOwnedAsync(int preferenceId, int userId);
    }
}
