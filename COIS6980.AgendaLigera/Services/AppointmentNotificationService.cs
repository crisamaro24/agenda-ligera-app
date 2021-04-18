using COIS6980.AgendaLigera.Models.Appointment;
using COIS6980.AgendaLigera.Models.Options;
using COIS6980.EFCoreDb.Models;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace COIS6980.AgendaLigera.Services
{
    public interface IAppointmentNotificationService
    {
        Task SendAppointmentReminders();
    }
    public class AppointmentNotificationService : IAppointmentNotificationService
    {
        private readonly ISendGridClient _emailClient;
        private readonly IAppointmentService _appointmentService;
        private readonly SendGridOptions _sendGridOptions;
        public AppointmentNotificationService(ISendGridClient emailClient, IAppointmentService appointmentService, IOptions<SendGridOptions> sendGridOptions)
        {
            _emailClient = emailClient;
            _appointmentService = appointmentService;
            _sendGridOptions = sendGridOptions.Value;
        }

        public async Task SendAppointmentReminders()
        {
            List<Appointment> upcomingAppointments = await _appointmentService.GetAppointmentsByDate(DateTime.UtcNow.AddDays(1));

            if (upcomingAppointments != null)
            {
                var appointmentReminders = upcomingAppointments
                    .Select(x => new AppointmentReminderDetails()
                    {
                        AppointmentId = x.AppointmentId,
                        When = x.ServiceSchedule.StartDate,
                        ServiceRecipientName = x.ServiceRecipient.FirstName,
                        ServiceRecipientEmail = x.ServiceRecipient.User.Email,
                        ServiceProviderName = x.ServiceSchedule.Service.Employee.FirstName,
                        ServiceName = x.ServiceSchedule.Service.Title
                    }).ToList();

                foreach (var appointment in appointmentReminders)
                {
                    await SendEmailToSingleRecipient(
                        appointment.ServiceRecipientEmail,
                        appointment.ServiceRecipientName,
                        "Recordatorio de cita",
                        "Te esperamos en nuestro establecimiento.",
                        "<strong>Te esperamos en nuestro establecimiento.</strong>");
                }
            }
        }

        public async Task<bool> SendEmailToSingleRecipient(string recipientEmail, string recipientName, string subject, string plainTextContent, string htmlContent)
        {
            var fromEmail = new EmailAddress(_sendGridOptions.SenderEmail, _sendGridOptions.SenderName);
            var toEmail = new EmailAddress(recipientEmail, recipientName);
            var emailMessage = MailHelper.CreateSingleEmail(fromEmail, toEmail, subject, plainTextContent, htmlContent);

            var response = await _emailClient.SendEmailAsync(emailMessage);

            return response?.IsSuccessStatusCode ?? false;
        }

        public async Task<bool> SendEmailTemplateToSingleRecipient()
        {
            throw new NotImplementedException();
        }
    }
}
