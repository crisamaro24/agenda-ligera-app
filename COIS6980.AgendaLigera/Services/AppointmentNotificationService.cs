using COIS6980.AgendaLigera.Models.Appointment;
using COIS6980.EFCoreDb.Models;
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
        public AppointmentNotificationService(ISendGridClient emailClient, IAppointmentService appointmentService)
        {
            _emailClient = emailClient;
            _appointmentService = appointmentService;
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
                    await SendEmailToSingleRecipient(appointment.ServiceRecipientEmail, appointment.ServiceRecipientName);
                }
            }
        }

        public async Task<bool> SendEmailToSingleRecipient(
            string recipientEmail = "thebuilderbob223@gmail.com",
            string recipientName = "Cristian",
            string subject = "Recordatorio de cita",
            string plainTextContent = "Te esperamos en nuestro establecimiento pronto.",
            string htmlContent = "<strong>Te esperamos en nuestro establecimiento pronto.</strong>")
        {
            var fromEmail = new EmailAddress("cristianamaro7@outlook.com", "Cristian Amaro");
            var toEmail = new EmailAddress(recipientEmail, recipientName);
            var emailMessage = MailHelper.CreateSingleEmail(fromEmail, toEmail, subject, plainTextContent, htmlContent);

            var response = await _emailClient.SendEmailAsync(emailMessage);

            return response?.IsSuccessStatusCode ?? false;
        }
    }
}
