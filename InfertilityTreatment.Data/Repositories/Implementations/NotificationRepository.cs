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
    public class NotificationRepository : BaseRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(ApplicationDbContext context) : base(context)
        {
        }
        public async Task<List<Notification>> GetNotificationsByUserIdAsync(
            int userId,
            DateTime? sentAtStart = null,
            DateTime? sentAtEnd = null,
            bool? isRead = null,
            string sortBy = "CreatedAtDesc")
        {
            IQueryable<Notification> query = _dbSet.Where(n => n.UserId == userId && n.IsActive);

            if (sentAtStart.HasValue)
            {
                query = query.Where(n => n.SentAt.HasValue && n.SentAt.Value >= sentAtStart.Value);
            }
            if (sentAtEnd.HasValue)
            {
                query = query.Where(n => n.SentAt.HasValue && n.SentAt.Value <= sentAtEnd.Value);
            }
            if (isRead.HasValue)
            {
                query = query.Where(n => n.IsRead == isRead.Value);
            }

            query = sortBy switch
            {
                "CreatedAtAsc" => query.OrderBy(n => n.CreatedAt),
                "TitleAsc" => query.OrderBy(n => n.Title),
                "TitleDesc" => query.OrderByDescending(n => n.Title),
                "SentAtAsc" => query.OrderBy(n => n.SentAt),
                "SentAtDesc" => query.OrderByDescending(n => n.SentAt),
                _ => query.OrderByDescending(n => n.CreatedAt),
            };

            return await query.ToListAsync();
        }
        public async Task<int> GetUnreadNotificationsCountAsync(int userId)
        {
            return await _dbSet
                .Where(n => n.UserId == userId && !n.IsRead && n.IsActive)
                .CountAsync();
        }
        public async Task<List<Notification>> GetScheduledPendingNotificationsAsync()
        {
            return await _dbSet
                .Where(n => n.IsActive &&
                            (n.ScheduledAt.HasValue && n.ScheduledAt.Value <= DateTime.UtcNow) &&
                            n.EmailStatus == EmailStatus.Pending)
                .ToListAsync();
        }
    }
}
