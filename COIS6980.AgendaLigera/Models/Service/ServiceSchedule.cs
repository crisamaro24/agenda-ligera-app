using System;

namespace COIS6980.AgendaLigera.Models.Service
{
    public class ServiceSchedule
    {
        public int ServiceScheduleId { get; set; }
        public string ScheduleDisplayText { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Capacity { get; set; }
        public int ScheduledCount { get; set; }
    }
}
