using System;

namespace COIS6980.AgendaLigera.Models.Appointment
{
    public class AppointmentReminderDetails
    {
        public int AppointmentId { get; set; }
        public DateTime When { get; set; }
        public string ServiceRecipientName { get; set; }
        public string ServiceRecipientEmail { get; set; }
        public string ServiceProviderName { get; set; }
        public string ServiceName { get; set; }
    }
}
