using System;
using System.Collections.Generic;

#nullable disable

namespace COIS6980.EFCoreDb.Models
{
    public partial class Employee
    {
        public Employee()
        {
            Services = new HashSet<Service>();
        }

        public int EmployeeId { get; set; }
        public string UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool? IsActive { get; set; }
        public bool IsDeleted { get; set; }

        public virtual AspNetUser User { get; set; }
        public virtual ICollection<Service> Services { get; set; }
    }
}
