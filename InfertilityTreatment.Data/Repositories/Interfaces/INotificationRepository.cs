using InfertilityTreatment.Entity.Entities;
using InfertilityTreatment.Data.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Data.Repositories.Interfaces
{
    public interface INotificationRepository : IBaseRepository<Notification>
    {
        Task<List<Notification>> GetNotificationsByUserIdAsync(
            int userId,
            DateTime? sentAtStart = null,
            DateTime? sentAtEnd = null,
            bool? isRead = null,
            string sortBy = "CreatedAtDesc");
        Task<int> GetUnreadNotificationsCountAsync(int userId);
        Task<List<Notification>> GetScheduledPendingNotificationsAsync();
    }
}
