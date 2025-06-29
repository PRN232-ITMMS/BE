using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Entity.Enums
{
    public enum EmailStatus : byte
    {
        Pending = 1,
        Queued = 2,
        Sent = 3,
        Failed = 4,
        DisabledByUser = 5 
    }
}
