using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Entity.Enums
{
    public enum NotificationType : byte
    {
        Reminder = 1,
        Appointment = 2,
        Result = 3,
        General = 4,
        Emergency = 5,
        TreatmentCycleStatusChange = 6,
        CriticalTestResult = 7
    }
}
