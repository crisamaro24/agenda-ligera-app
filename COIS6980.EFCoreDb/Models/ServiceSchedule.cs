using System;
using System.Collections.Generic;

#nullable disable

namespace COIS6980.EFCoreDb.Models
{
    public partial class ServiceSchedule
    {
        public ServiceSchedule()
        {
            Appointments = new HashSet<Appointment>();
        }

        public int ServiceScheduleId { get; set; }
        public int ServiceId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? Capacity { get; set; }
        public bool? IsActive { get; set; }
        public bool IsDeleted { get; set; }

        public virtual Service Service { get; set; }
        public virtual ICollection<Appointment> Appointments { get; set; }
    }
}
