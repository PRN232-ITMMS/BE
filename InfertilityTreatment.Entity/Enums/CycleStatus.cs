using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Entity.Enums
{
    public enum CycleStatus : byte
    {
        Created = 1,
        Initialized = 2,
        InProgress = 3,
        Completed = 4,
        Cancelled = 5,
    }
}
