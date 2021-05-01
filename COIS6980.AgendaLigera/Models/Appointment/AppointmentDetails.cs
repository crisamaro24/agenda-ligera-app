namespace COIS6980.AgendaLigera.Models.Appointment
{
    public class AppointmentDetails
    {
        public string FormattedDate { get; set; }
        public string FormattedTime { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public string ServiceRecipientName { get; set; }
        public string ServiceProviderName { get; set; }
    }
}
