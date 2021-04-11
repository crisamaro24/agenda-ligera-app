using System;
using System.Collections.Generic;

#nullable disable

namespace COIS6980.EFCoreDb.Models
{
    public partial class Service
    {
        public Service()
        {
            ServiceSchedules = new HashSet<ServiceSchedule>();
        }

        public int ServiceId { get; set; }
        public int EmployeeId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int? EstimatedDurationInMinutes { get; set; }
        public bool? IsActive { get; set; }
        public bool IsDeleted { get; set; }

        public virtual Employee Employee { get; set; }
        public virtual ICollection<ServiceSchedule> ServiceSchedules { get; set; }
    }
}
