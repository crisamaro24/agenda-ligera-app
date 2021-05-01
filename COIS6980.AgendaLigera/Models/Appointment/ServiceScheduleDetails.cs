using System;

namespace COIS6980.AgendaLigera.Models.Appointment
{
    public class ServiceScheduleDetails
    {
        public int ServiceScheduleId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? Capacity { get; set; }
    }
}
