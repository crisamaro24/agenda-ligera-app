using COIS6980.AgendaLigera.Models.Appointment;
using COIS6980.EFCoreDb.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        Task<List<EmployeeServiceAppointments>> GetEmployeeServiceAppointmentsByDate(
            DateTime date,
            int? employeeId = null,
            bool active = true,
            bool deleted = false);
        Task<AppointmentDetails> GetAppointmentDetails(int appointmentId);
        Task CancelAppointment(int appointmentId);
        Task<List<ServiceScheduleDetails>> GetAvailableServiceAppointmentsBetweenDates(int serviceId, DateTime startDate, DateTime endDate);
        Task RescheduleAppointment(int appointmentId, int newServiceScheduleId);
        Task<List<AppointmentDetails>> GetAppointmentDetailsListByDate(
            DateTime date,
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
                .Include(x => x.ServiceSchedule)
                    .ThenInclude(x => x.Service)
                    .ThenInclude(x => x.Employee)
                .Where(x => x.IsActive == active && x.IsDeleted == deleted)
                .Where(x => x.ServiceSchedule.StartDate.Date >= startDate.Date)
                .Where(x => x.ServiceSchedule.EndDate.Date <= endDate.Date);

            if (!string.IsNullOrWhiteSpace(userId))
            {
                appointmentsQuery = appointmentsQuery
                    .Where(x => x.ServiceRecipient.UserId == userId
                    || x.ServiceSchedule.Service.Employee.UserId == userId);
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

        public async Task<List<EmployeeServiceAppointments>> GetEmployeeServiceAppointmentsByDate(
            DateTime date,
            int? employeeId = null,
            bool active = true,
            bool deleted = false)
        {
            var appointmentsQuery = _agendaLigeraCtx.Appointments
                .Include(x => x.ServiceRecipient)
                .Include(x => x.ServiceSchedule)
                    .ThenInclude(x => x.Service)
                    .ThenInclude(x => x.Employee)
                .Where(x => x.IsActive == active && x.IsDeleted == deleted)
                .Where(x => x.ServiceSchedule.StartDate.Date == date);

            if (employeeId != null)
            {
                appointmentsQuery = appointmentsQuery
                    .Where(x => x.ServiceSchedule.Service.Employee.EmployeeId == employeeId);
            }

            var appointmentsFound = await appointmentsQuery.OrderBy(x => x.ServiceSchedule.StartDate).ToListAsync();

            if ((appointmentsFound?.Count ?? 0) == 0)
                return new List<EmployeeServiceAppointments>();

            // Initialize list of appointments to return
            var employeeServiceAppointments = new List<EmployeeServiceAppointments>();

            foreach (var appointment in appointmentsFound)
            {
                employeeId ??= appointment.ServiceSchedule.Service.EmployeeId;
                var serviceId = appointment.ServiceSchedule.ServiceId;
                var customerName = appointment.ServiceRecipient.FirstName + " " + appointment.ServiceRecipient.LastName;
                var startTime = appointment.ServiceSchedule.StartDate.ToString("hh:mm tt");

                var repeatedEmployeeService = employeeServiceAppointments
                    .FirstOrDefault(x => x.EmployeeId == employeeId.Value
                    && x.ServiceId == serviceId);

                if (repeatedEmployeeService == null)
                {
                    var serviceName = appointment.ServiceSchedule.Service.Title;
                    var employeeName = appointment.ServiceSchedule.Service.Employee.FirstName + " " +
                        appointment.ServiceSchedule.Service.Employee.LastName;

                    employeeServiceAppointments.Add(new EmployeeServiceAppointments()
                    {
                        ServiceTitle = serviceName + " (" + employeeName + ")",
                        Customers = new List<ServiceCustomer>()
                        {
                            new ServiceCustomer()
                            {
                                ServiceCustomerDescription = startTime + " - " + customerName,
                                AppointmentId = appointment.AppointmentId,
                                ServiceScheduleId = appointment.ServiceSchedule.ServiceScheduleId,
                                ServiceRecipientId = appointment.ServiceRecipient.ServiceRecipientId
                            }
                        },
                        ServiceId = serviceId,
                        EmployeeId = employeeId.Value
                    });
                }
                else
                {
                    repeatedEmployeeService.Customers.Add(new ServiceCustomer()
                    {
                        ServiceCustomerDescription = startTime + " - " + customerName,
                        AppointmentId = appointment.AppointmentId,
                        ServiceScheduleId = appointment.ServiceSchedule.ServiceScheduleId,
                        ServiceRecipientId = appointment.ServiceRecipient.ServiceRecipientId
                    });
                }
            }

            return employeeServiceAppointments;
        }

        public async Task<AppointmentDetails> GetAppointmentDetails(int appointmentId)
        {
            var appointment = await _agendaLigeraCtx.Appointments
                .Include(x => x.ServiceRecipient)
                .Include(x => x.ServiceSchedule)
                    .ThenInclude(x => x.Service)
                    .ThenInclude(x => x.Employee)
                .Where(x => x.AppointmentId == appointmentId)
                .Where(x => x.IsActive == true && x.IsDeleted == false)
                .FirstOrDefaultAsync();

            if (appointment == null)
                return new AppointmentDetails();

            var appointmentCapacity = appointment.ServiceSchedule.Capacity;

            var appointmentDate = appointment.ServiceSchedule.StartDate;
            var appointmentTime = appointmentDate.ToString("hh:mm tt") +
                ((appointmentCapacity ?? 0) == 1 ? (" - " + appointment.ServiceSchedule.EndDate.ToString("hh:mm tt")) : string.Empty);

            var customerName = appointment.ServiceRecipient.FirstName + " " + appointment.ServiceRecipient.LastName;
            var employeeName = appointment.ServiceSchedule.Service.Employee.FirstName + " " + appointment.ServiceSchedule.Service.Employee.LastName;

            return new AppointmentDetails()
            {
                AppointmentId = appointmentId,
                ServiceId = appointment.ServiceSchedule.ServiceId,
                StartDate = appointmentDate,
                EndDate = appointment.ServiceSchedule.EndDate,
                FormattedDate = appointmentDate.Date.ToString("D", new CultureInfo("es-ES")),
                FormattedTime = appointmentTime,
                ServiceName = appointment.ServiceSchedule.Service.Title,
                ServiceRecipientName = customerName,
                ServiceProviderName = employeeName
            };
        }

        public async Task CancelAppointment(int appointmentId)
        {
            var appointment = await _agendaLigeraCtx.Appointments
                .FirstOrDefaultAsync(x => x.AppointmentId == appointmentId && x.IsActive == true && x.IsDeleted == false);

            if (appointment != null)
            {
                appointment.IsActive = false;
                _agendaLigeraCtx.Entry(appointment).State = EntityState.Modified;
                await _agendaLigeraCtx.SaveChangesAsync();
            }
        }

        public async Task<List<ServiceScheduleDetails>> GetAvailableServiceAppointmentsBetweenDates(int serviceId, DateTime startDate, DateTime endDate)
        {
            var today = DateTime.UtcNow.ToLocalTime();

            var scheduledAppointments = await _agendaLigeraCtx.Appointments
                .Include(x => x.ServiceRecipient)
                .Include(x => x.ServiceSchedule)
                    .ThenInclude(x => x.Service)
                .Where(x => x.IsActive == true && x.IsDeleted == false)
                .Where(x => x.ServiceSchedule.ServiceId == serviceId)
                .Where(x => x.ServiceSchedule.StartDate > today)
                .Where(x => x.ServiceSchedule.StartDate.Date >= startDate.Date)
                .Where(x => x.ServiceSchedule.EndDate.Date <= endDate.Date)
                .ToListAsync();

            var futureServiceSchedules = await _agendaLigeraCtx.ServiceSchedules
                .Include(x => x.Service)
                .Where(x => x.IsActive == true && x.IsDeleted == false)
                .Where(x => x.ServiceId == serviceId)
                .Where(x => x.StartDate > today)
                .Where(x => x.StartDate.Date >= startDate.Date)
                .Where(x => x.EndDate.Date <= endDate.Date)
                .OrderBy(x => x.StartDate)
                .ToListAsync();

            var availableServiceSchedules = futureServiceSchedules
                .Where(x => x.Capacity == null || x.Capacity > scheduledAppointments.Where(y => y.ServiceScheduleId == x.ServiceScheduleId).Count())
                .ToList();

            var availableServiceAppointments = availableServiceSchedules
                .Select(x => new ServiceScheduleDetails()
                {
                    ServiceScheduleId = x.ServiceScheduleId,
                    FormattedDate = x.StartDate.Date.ToString("D", new CultureInfo("es-ES")),
                    FormattedTime = x.StartDate.ToString("hh:mm tt") + ((x.Capacity ?? 0) == 1 ? (" - " + x.EndDate.ToString("hh:mm tt")) : string.Empty),
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                    Capacity = x.Capacity
                }).ToList();

            return availableServiceAppointments;
        }

        public async Task RescheduleAppointment(int appointmentId, int newServiceScheduleId)
        {
            var appointment = await _agendaLigeraCtx.Appointments
                .FirstOrDefaultAsync(x => x.AppointmentId == appointmentId && x.IsActive == true && x.IsDeleted == false);

            if (appointment != null)
            {
                appointment.ServiceScheduleId = newServiceScheduleId;
                _agendaLigeraCtx.Entry(appointment).State = EntityState.Modified;
                await _agendaLigeraCtx.SaveChangesAsync();
            }
        }

        public async Task<List<AppointmentDetails>> GetAppointmentDetailsListByDate(
            DateTime date,
            string userId = null,
            bool active = true,
            bool deleted = false)
        {
            var appointmentsQuery = _agendaLigeraCtx.Appointments
                .Include(x => x.ServiceRecipient)
                .Include(x => x.ServiceSchedule)
                    .ThenInclude(x => x.Service)
                    .ThenInclude(x => x.Employee)
                .Where(x => x.IsActive == active && x.IsDeleted == deleted)
                .Where(x => x.ServiceSchedule.StartDate.Date == date.Date);

            if (!string.IsNullOrWhiteSpace(userId))
            {
                var userRole = await _agendaLigeraCtx.AspNetUserRoles
                    .Include(x => x.Role)
                    .Where(x => x.UserId == userId)
                    .Select(x => x.Role.Name)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrWhiteSpace(userRole))
                    return new List<AppointmentDetails>();

                switch (userRole.ToLowerInvariant())
                {
                    case "paciente":
                        appointmentsQuery = appointmentsQuery
                            .Where(x => x.ServiceRecipient.UserId == userId);
                        break;
                    case "doctora":
                        appointmentsQuery = appointmentsQuery
                            .Where(x => x.ServiceSchedule.Service.Employee.UserId == userId);
                        break;
                    default:
                        break;
                }
            }

            var appointmentsFound = await appointmentsQuery.OrderBy(x => x.ServiceSchedule.StartDate).ToListAsync();

            if ((appointmentsFound?.Count ?? 0) == 0)
                return new List<AppointmentDetails>();

            var appointments = appointmentsFound
                .Select(x => new AppointmentDetails()
                {
                    AppointmentId = x.AppointmentId,
                    ServiceId = x.ServiceSchedule.ServiceId,
                    StartDate = x.ServiceSchedule.StartDate,
                    EndDate = x.ServiceSchedule.EndDate,
                    FormattedDate = x.ServiceSchedule.StartDate.Date.ToString("D", new CultureInfo("es-ES")),
                    FormattedTime = x.ServiceSchedule.StartDate.ToString("hh:mm tt"),
                    ServiceName = x.ServiceSchedule.Service.Title,
                    ServiceRecipientName = x.ServiceRecipient.FirstName + " " + x.ServiceRecipient.LastName,
                    ServiceProviderName = x.ServiceSchedule.Service.Employee.FirstName + " " + x.ServiceSchedule.Service.Employee.LastName
                }).ToList();

            return appointments;
        }
    }
}
