using COIS6980.AgendaLigera.Models.Appointment;
using COIS6980.EFCoreDb.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace COIS6980.AgendaLigera.Services
{
    public interface IAppointmentService
    {
        Task<List<Appointment>> GetAppointmentsByDate(DateTime appointmentDate);
        Task<List<AppointmentCalendarDetails>> GetAppointmentsBetweenDates(
            DateTime startDate,
            DateTime endDate,
            string userId = null,
            bool active = true,
            bool deleted = false);
    }
    public class AppointmentService : IAppointmentService
    {
        private readonly AgendaLigeraContext _agendaLigeraCtx;
        public AppointmentService(AgendaLigeraContext agendaLigeraCtx)
        {
            _agendaLigeraCtx = agendaLigeraCtx;
        }

        public async Task<List<Appointment>> GetAppointmentsByDate(DateTime appointmentDate)
        {
            var appointments =
                await _agendaLigeraCtx.Appointments
                .Include(x => x.ServiceRecipient)
                    .ThenInclude(x => x.User)
                .Include(x => x.ServiceSchedule)
                    .ThenInclude(x => x.Service)
                    .ThenInclude(x => x.Employee)
                .Where(x => x.IsActive == true && x.IsDeleted == false)
                .Where(x => x.ServiceSchedule.StartDate.Date == appointmentDate.Date)
                .ToListAsync();

            return appointments;
        }

        public async Task<List<AppointmentCalendarDetails>> GetAppointmentsBetweenDates(
            DateTime startDate,
            DateTime endDate,
            string userId = null,
            bool active = true,
            bool deleted = false)
        {
            if (startDate > endDate)
                return new List<AppointmentCalendarDetails>();

            var appointmentsQuery = _agendaLigeraCtx.Appointments
                .Include(x => x.ServiceRecipient)
                    .ThenInclude(x => x.User)
                .Include(x => x.ServiceSchedule)
                    .ThenInclude(x => x.Service)
                    .ThenInclude(x => x.Employee)
                .Where(x => x.IsActive == active && x.IsDeleted == deleted)
                .Where(x => x.ServiceSchedule.StartDate.Date >= startDate.Date)
                .Where(x => x.ServiceSchedule.EndDate.Date <= endDate.Date);

            if (!string.IsNullOrWhiteSpace(userId))
            {
                appointmentsQuery = appointmentsQuery
                    .Where(x => x.ServiceRecipient.User.Id == userId || x.ServiceSchedule.Service.Employee.User.Id == userId);
            }

            var appointmentsFound = await appointmentsQuery.ToListAsync();

            if ((appointmentsFound?.Count ?? 0) == 0)
                return new List<AppointmentCalendarDetails>();

            var calendarAppointments = appointmentsFound
                .Select(x => new AppointmentCalendarDetails()
                {
                    Start = x.ServiceSchedule.StartDate,
                    End = x.ServiceSchedule.EndDate,
                    Title = x.ServiceSchedule.Service.Title,

                    AppointmentId = x.AppointmentId,
                    ServiceId = x.ServiceSchedule.Service.ServiceId,
                    ServiceScheduleId = x.ServiceSchedule.ServiceScheduleId,
                    ServiceRecipientId = x.ServiceRecipient.ServiceRecipientId,
                    ServiceProviderEmployeeId = x.ServiceSchedule.Service.Employee.EmployeeId
                }).ToList();

            return calendarAppointments;
        }

        // Add method to get appointments based on specified date and employeeId
        // Return List<EmployeeServiceAppointments>
    }
}
