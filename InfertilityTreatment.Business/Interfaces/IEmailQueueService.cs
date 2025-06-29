using InfertilityTreatment.Entity.DTOs.Emails;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Business.Interfaces
{
    public interface IEmailQueueService
    {
        void EnqueueEmail(QueuedEmailItemDto emailItem);
        QueuedEmailItemDto? DequeueEmail();
        int QueueCount { get; }
    }
}
