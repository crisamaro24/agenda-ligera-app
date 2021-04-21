using System;

namespace COIS6980.AgendaLigera.Models.Appointment
{
    public class AppointmentCalendarDetails
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Title { get; set; }

        // To help display additional details or to edit the appointment
        public int AppointmentId { get; set; }
        public int ServiceId { get; set; }
        public int ServiceScheduleId { get; set; }
        public int ServiceRecipientId { get; set; }
        public int ServiceProviderEmployeeId { get; set; }
    }
}
