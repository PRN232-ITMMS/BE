using InfertilityTreatment.Business.Interfaces;
using InfertilityTreatment.Entity.DTOs.Emails;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Business.Services
{
    public class EmailQueueService : IEmailQueueService
    {
        private readonly ConcurrentQueue<QueuedEmailItemDto> _emailQueue = new ConcurrentQueue<QueuedEmailItemDto>();
        public void EnqueueEmail(QueuedEmailItemDto emailItem)
        {
            _emailQueue.Enqueue(emailItem);
        }
        public QueuedEmailItemDto? DequeueEmail()
        {
            _emailQueue.TryDequeue(out var emailItem);
            return emailItem;
        }
        public int QueueCount => _emailQueue.Count;
    }
}
