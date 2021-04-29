using System.Collections.Generic;

namespace COIS6980.AgendaLigera.Models.Appointment
{
    public class EmployeeServiceAppointments
    {
        public string ServiceTitle { get; set; }
        public IList<ServiceCustomer> Customers { get; set; }
        public int ServiceId { get; set; }
        public int EmployeeId { get; set; }
    }
}