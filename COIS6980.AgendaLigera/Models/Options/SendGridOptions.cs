﻿namespace COIS6980.AgendaLigera.Models.Options
{
    public class SendGridOptions
    {
        public string ApiKey { get; set; }
        public string CompanyName { get; set; }
        public string SenderName { get; set; }
        public string SenderEmail { get; set; }
        public string ReminderEmailTemplateId { get; set; }
    }
}
