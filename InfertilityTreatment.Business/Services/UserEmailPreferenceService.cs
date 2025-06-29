using InfertilityTreatment.Business.Interfaces;
using InfertilityTreatment.Data.Repositories.Interfaces;
using InfertilityTreatment.Entity.DTOs.Emails;
using InfertilityTreatment.Entity.Entities;
using InfertilityTreatment.Entity.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Business.Services
{
    public class UserEmailPreferenceService : IUserEmailPreferenceService
    {
        private readonly IBaseRepository<UserEmailPreference> _userEmailPreferenceRepository;
        private readonly IBaseRepository<User> _userRepository;
        private readonly ILogger<UserEmailPreferenceService> _logger;

        public UserEmailPreferenceService(
            IBaseRepository<UserEmailPreference> userEmailPreferenceRepository,
            IBaseRepository<User> userRepository,
            ILogger<UserEmailPreferenceService> logger)
        {
            _userEmailPreferenceRepository = userEmailPreferenceRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<UserEmailPreferenceResponseDto?> CreateUserEmailPreferenceAsync(CreateUserEmailPreferenceDto createDto)
        {
            var userExists = await _userRepository.ExistsAsync(createDto.UserId);
            if (!userExists)
            {
                _logger.LogWarning("Không thể tạo sở thích email: User với ID {UserId} không tồn tại.", createDto.UserId);
                return null;
            }

            var existingPreference = await _userEmailPreferenceRepository
                .FirstOrDefaultAsync(p => p.UserId == createDto.UserId && p.NotificationType == createDto.NotificationType && p.IsActive);

            if (existingPreference != null)
            {
                _logger.LogWarning("Không thể tạo sở thích email: Sở thích cho User {UserId} và loại thông báo '{NotificationType}' đã tồn tại.", createDto.UserId, createDto.NotificationType);
                return null;
            }

            var preference = new UserEmailPreference
            {
                UserId = createDto.UserId,
                NotificationType = createDto.NotificationType,
                IsEnabled = createDto.IsEnabled,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _userEmailPreferenceRepository.AddAsync(preference);
            var saved = await _userEmailPreferenceRepository.SaveChangesAsync();

            if (saved > 0)
            {
                _logger.LogInformation("Đã tạo sở thích email mới cho User {UserId}, Loại: {NotificationType}, Bật: {IsEnabled}",
                    preference.UserId, preference.NotificationType, preference.IsEnabled);
                return MapToResponseDto(preference);
            }
            _logger.LogError("Không thể lưu sở thích email mới vào cơ sở dữ liệu.");
            return null;
        }

        public async Task<UserEmailPreference?> GetUserPreferenceAsync(int userId, NotificationType notificationType)
        {
            return await _userEmailPreferenceRepository.FirstOrDefaultAsync(p => p.UserId == userId && p.NotificationType == notificationType && p.IsActive);
        }

        public async Task<List<UserEmailPreference>> GetUserAllPreferencesAsync(int userId)
        {
            return (await _userEmailPreferenceRepository.FindAsync(p => p.UserId == userId && p.IsActive)).ToList();
        }

        public async Task<UserEmailPreferenceResponseDto?> GetUserEmailPreferenceByIdAsync(int preferenceId, int userId)
        {
            var preference = await _userEmailPreferenceRepository.FirstOrDefaultAsync(p => p.Id == preferenceId && p.UserId == userId && p.IsActive);
            return preference != null ? MapToResponseDto(preference) : null;
        }

        public async Task<bool> UpdateUserPreferenceAsync(UpdateUserEmailPreferenceDto updateDto)
        {
            var existingPreference = await _userEmailPreferenceRepository.FirstOrDefaultAsync(p => p.Id == updateDto.Id && p.UserId == updateDto.UserId && p.IsActive);

            if (existingPreference == null)
            {
                _logger.LogWarning("Không tìm thấy sở thích email với ID {Id} cho User {UserId} để cập nhật.", updateDto.Id, updateDto.UserId);
                return false;
            }

            if (existingPreference.NotificationType != updateDto.NotificationType)
            {
                var conflictPreference = await _userEmailPreferenceRepository
                    .FirstOrDefaultAsync(p => p.UserId == updateDto.UserId && p.NotificationType == updateDto.NotificationType && p.Id != updateDto.Id && p.IsActive);
                if (conflictPreference != null)
                {
                    _logger.LogWarning("Không thể cập nhật sở thích email: Sở thích cho User {UserId} và loại thông báo '{NotificationType}' đã tồn tại.", updateDto.UserId, updateDto.NotificationType);
                    return false;
                }
            }

            existingPreference.NotificationType = updateDto.NotificationType;
            existingPreference.IsEnabled = updateDto.IsEnabled;
            existingPreference.UpdatedAt = DateTime.UtcNow;

            await _userEmailPreferenceRepository.UpdateAsync(existingPreference);
            return await _userEmailPreferenceRepository.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteUserEmailPreferenceAsync(int preferenceId, int userId)
        {
            var preference = await _userEmailPreferenceRepository.FirstOrDefaultAsync(p => p.Id == preferenceId && p.UserId == userId && p.IsActive);
            if (preference == null)
            {
                _logger.LogWarning("Không tìm thấy sở thích email với ID {PreferenceId} cho User {UserId} để xóa.", preferenceId, userId);
                return false;
            }

            await _userEmailPreferenceRepository.DeleteAsync(preferenceId);
            return await _userEmailPreferenceRepository.SaveChangesAsync() > 0;
        }

        public async Task<bool> IsEmailNotificationEnabledAsync(int userId, NotificationType notificationType)
        {
            var preference = await GetUserPreferenceAsync(userId, notificationType);
            return preference != null && preference.IsEnabled;
        }

        public async Task<UserEmailPreference?> GetUserEmailPreferenceIfOwnedAsync(int preferenceId, int userId)
        {
            return await _userEmailPreferenceRepository.FirstOrDefaultAsync(n => n.Id == preferenceId && n.UserId == userId && n.IsActive);
        }

        private UserEmailPreferenceResponseDto MapToResponseDto(UserEmailPreference preference)
        {
            return new UserEmailPreferenceResponseDto
            {
                Id = preference.Id,
                UserId = preference.UserId,
                NotificationType = preference.NotificationType.ToString(),
                IsEnabled = preference.IsEnabled,
                CreatedAt = preference.CreatedAt,
                UpdatedAt = preference.UpdatedAt
            };
        }
    }
}
