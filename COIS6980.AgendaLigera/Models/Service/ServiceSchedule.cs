using System;

namespace COIS6980.AgendaLigera.Models.Service
{
    public class ServiceSchedule
    {
        public int ServiceScheduleId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Capacity { get; set; }
        public int ScheduledCount { get; set; }
        public string FormattedStartDate { get; set; }
        public string FormattedEndDate { get; set; }
        public string FormattedStartTime { get; set; }
        public string FormattedEndTime { get; set; }
    }
}
