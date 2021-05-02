using System;

namespace COIS6980.AgendaLigera.Models.Appointment
{
    public class AppointmentDetails
    {
        public int ServiceId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string FormattedDate { get; set; }
        public string FormattedTime { get; set; }
        public string ServiceName { get; set; }
        public string ServiceRecipientName { get; set; }
        public string ServiceProviderName { get; set; }
    }
}
