using Newtonsoft.Json;

namespace COIS6980.AgendaLigera.Models.Appointment
{
    public class ReminderEmailTemplateData
    {
        [JsonProperty("serviceName")]
        public string ServiceName { get; set; }

        [JsonProperty("when")]
        public string When { get; set; }

        [JsonProperty("customerName")]
        public string CustomerName { get; set; }

        [JsonProperty("employeeName")]
        public string EmployeeName { get; set; }

        [JsonProperty("companyName")]
        public string CompanyName { get; set; }
    }
}
