namespace COIS6980.AgendaLigera.Models.Appointment
{
    public class ServiceCustomer
    {
        public string ServiceCustomerDescription { get; set; }

        // To help display additional details or to edit the appointment
        public int AppointmentId { get; set; }
        public int ServiceScheduleId { get; set; }
        public int ServiceRecipientId { get; set; }
    }
}
