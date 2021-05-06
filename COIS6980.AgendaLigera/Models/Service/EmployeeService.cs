namespace COIS6980.AgendaLigera.Models.Service
{
    public class EmployeeService
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public string ServiceDescription { get; set; }
        public int EstimatedDurationInMinutes { get; set; }
        public bool IsActive { get; set; }
    }
}
