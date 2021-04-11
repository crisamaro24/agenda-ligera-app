using System;
using System.Collections.Generic;

#nullable disable

namespace COIS6980.EFCoreDb.Models
{
    public partial class Appointment
    {
        public int AppointmentId { get; set; }
        public int ServiceRecipientId { get; set; }
        public int ServiceScheduleId { get; set; }
        public bool? IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedDate { get; set; }

        public virtual ServiceRecipient ServiceRecipient { get; set; }
        public virtual ServiceSchedule ServiceSchedule { get; set; }
    }
}
