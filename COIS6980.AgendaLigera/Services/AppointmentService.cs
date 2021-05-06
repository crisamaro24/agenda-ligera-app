using COIS6980.AgendaLigera.Models.Appointment;
using COIS6980.EFCoreDb.Models;
using Microsoft.EntityFrameworkCore;
using MoreLinq;
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
            string userId = null,
            bool active = true,
            bool deleted = false);
        Task<AppointmentDetails> GetAppointmentDetails(int appointmentId);
        Task CancelAppointment(int appointmentId);
        Task<List<ServiceScheduleDetails>> GetAvailableServiceAppointmentsBetweenDates(
            int serviceId,
            DateTime startDate,
            DateTime endDate);
        Task RescheduleAppointment(int appointmentId, int newServiceScheduleId);
        Task<List<AppointmentDetails>> GetAppointmentDetailsListByDate(
            DateTime date,
            string userId = null,
            bool active = true,
            bool deleted = false);
        Task<string> GetUserRole(string userId);
        Task<List<EmployeeDetails>> GetServiceProvidersForBooking(string userId = null);
        Task<List<ServiceRecipientDetails>> GetServiceRecipientsForBooking(string userId = null);
        Task<List<ServiceDetails>> GetEmployeeServicesForBooking(int employeeId);
        Task<List<ServiceScheduleDetails>> GetAvailableServiceAppointmentsForBooking(int serviceId, DateTime date);
        Task BookAppointment(int serviceRecipientId, int serviceScheduleId);
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
                .Include(x => x.ServiceSchedule)
                    .ThenInclude(x => x.Service)
                    .ThenInclude(x => x.Employee)
                .Where(x => x.IsActive == active && x.IsDeleted == deleted)
                .Where(x => x.ServiceSchedule.StartDate.Date >= startDate.Date)
                .Where(x => x.ServiceSchedule.EndDate.Date <= endDate.Date);

            if (!string.IsNullOrWhiteSpace(userId))
            {
                var userRole = await GetUserRole(userId);

                if (string.IsNullOrWhiteSpace(userRole))
                    return new List<AppointmentCalendarDetails>();

                if (userRole.ToLowerInvariant() == "doctora")
                    appointmentsQuery = appointmentsQuery
                            .Where(x => x.ServiceSchedule.Service.Employee.UserId == userId);
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
                    ServiceScheduleId = x.ServiceScheduleId,
                    ServiceRecipientId = x.ServiceRecipientId,
                    ServiceProviderEmployeeId = x.ServiceSchedule.Service.Employee.EmployeeId
                }).ToList();

            return calendarAppointments;
        }

        public async Task<List<EmployeeServiceAppointments>> GetEmployeeServiceAppointmentsByDate(
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
                .Where(x => x.ServiceSchedule.StartDate.Date == date);

            if (!string.IsNullOrWhiteSpace(userId))
            {
                var userRole = await GetUserRole(userId);

                if (string.IsNullOrWhiteSpace(userRole))
                    return new List<EmployeeServiceAppointments>();

                if (userRole.ToLowerInvariant() == "doctora")
                    appointmentsQuery = appointmentsQuery
                            .Where(x => x.ServiceSchedule.Service.Employee.UserId == userId);
            }

            var appointmentsFound = await appointmentsQuery.OrderBy(x => x.ServiceSchedule.StartDate).ToListAsync();

            if ((appointmentsFound?.Count ?? 0) == 0)
                return new List<EmployeeServiceAppointments>();

            // Initialize list of appointments to return
            var employeeServiceAppointments = new List<EmployeeServiceAppointments>();

            foreach (var appointment in appointmentsFound)
            {
                var employeeId = appointment.ServiceSchedule.Service.EmployeeId;
                var serviceId = appointment.ServiceSchedule.ServiceId;
                var customerName = appointment.ServiceRecipient.FirstName + " " + appointment.ServiceRecipient.LastName;
                var startTime = appointment.ServiceSchedule.StartDate.ToString("hh:mm tt");

                var repeatedEmployeeService = employeeServiceAppointments
                    .FirstOrDefault(x => x.EmployeeId == employeeId
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
                                ServiceScheduleId = appointment.ServiceScheduleId,
                                ServiceRecipientId = appointment.ServiceRecipientId
                            }
                        },
                        ServiceId = serviceId,
                        EmployeeId = employeeId
                    });
                }
                else
                {
                    repeatedEmployeeService.Customers.Add(new ServiceCustomer()
                    {
                        ServiceCustomerDescription = startTime + " - " + customerName,
                        AppointmentId = appointment.AppointmentId,
                        ServiceScheduleId = appointment.ServiceScheduleId,
                        ServiceRecipientId = appointment.ServiceRecipientId
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

        public async Task<List<ServiceScheduleDetails>> GetAvailableServiceAppointmentsBetweenDates(
            int serviceId,
            DateTime startDate,
            DateTime endDate)
        {
            var today = DateTime.UtcNow.ToLocalTime();

            var scheduledAppointments = await _agendaLigeraCtx.Appointments
                .Include(x => x.ServiceSchedule)
                .Where(x => x.IsActive == true && x.IsDeleted == false)
                .Where(x => x.ServiceSchedule.ServiceId == serviceId)
                .Where(x => x.ServiceSchedule.StartDate > today)
                .Where(x => x.ServiceSchedule.StartDate.Date >= startDate.Date)
                .Where(x => x.ServiceSchedule.EndDate.Date <= endDate.Date)
                .ToListAsync();

            var futureServiceSchedules = await _agendaLigeraCtx.ServiceSchedules
                .Include(x => x.Service)
                .Where(x => x.IsActive == true && x.IsDeleted == false)
                .Where(x => x.Service.IsActive == true && x.IsDeleted == false)
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
                var userRole = await GetUserRole(userId);

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

        public async Task<string> GetUserRole(string userId)
        {
            var userRole = await _agendaLigeraCtx.AspNetUserRoles
                .Include(x => x.Role)
                .Where(x => x.UserId == userId)
                .Select(x => x.Role.Name)
                .FirstOrDefaultAsync();

            return userRole;
        }

        public async Task<List<EmployeeDetails>> GetServiceProvidersForBooking(string userId = null)
        {
            DateTime currentTime = DateTime.UtcNow.ToLocalTime();

            var serviceSchedulesQuery = _agendaLigeraCtx.ServiceSchedules
                .Include(x => x.Service)
                    .ThenInclude(x => x.Employee)
                .Where(x => x.IsActive == true && x.IsDeleted == false)
                .Where(x => x.Service.IsActive == true && x.IsDeleted == false)
                .Where(x => x.Service.Employee.IsActive == true && x.Service.Employee.IsDeleted == false)
                .Where(x => x.StartDate > currentTime);

            if (!string.IsNullOrWhiteSpace(userId))
            {
                var userRole = await GetUserRole(userId);

                if (string.IsNullOrWhiteSpace(userRole))
                    return new List<EmployeeDetails>();

                if (userRole.ToLowerInvariant() == "doctora")
                    serviceSchedulesQuery = serviceSchedulesQuery
                            .Where(x => x.Service.Employee.UserId == userId);
            }

            var serviceSchedules = await serviceSchedulesQuery.ToListAsync();

            if ((serviceSchedules?.Count ?? 0) == 0)
                return new List<EmployeeDetails>();

            var serviceProviders = serviceSchedules
                .Select(x => new EmployeeDetails()
                {
                    EmployeeId = x.Service.EmployeeId,
                    EmployeeName = x.Service.Employee.FirstName + " " + x.Service.Employee.LastName
                })
                .DistinctBy(x => x.EmployeeId)
                .OrderBy(x => x.EmployeeName)
                .ToList();

            return serviceProviders;
        }

        public async Task<List<ServiceRecipientDetails>> GetServiceRecipientsForBooking(string userId = null)
        {
            var serviceRecipientsQuery = _agendaLigeraCtx.ServiceRecipients
                .Where(x => x.IsActive == true && x.IsDeleted == false);

            if (!string.IsNullOrWhiteSpace(userId))
            {
                var userRole = await GetUserRole(userId);

                if (string.IsNullOrWhiteSpace(userRole))
                    return new List<ServiceRecipientDetails>();

                if (userRole.ToLowerInvariant() == "paciente")
                    serviceRecipientsQuery = serviceRecipientsQuery
                            .Where(x => x.UserId == userId);
            }

            var serviceRecipientsFound = await serviceRecipientsQuery.OrderBy(x => x.FirstName).ToListAsync();

            if ((serviceRecipientsFound?.Count ?? 0) == 0)
                return new List<ServiceRecipientDetails>();

            var serviceRecipients = serviceRecipientsFound
                .Select(x => new ServiceRecipientDetails()
                {
                    ServiceRecipientId = x.ServiceRecipientId,
                    ServiceRecipientName = x.FirstName + " " + x.LastName
                }).ToList();

            return serviceRecipients;
        }

        public async Task<List<ServiceDetails>> GetEmployeeServicesForBooking(int employeeId)
        {
            DateTime currentTime = DateTime.UtcNow.ToLocalTime();

            var serviceSchedules = await _agendaLigeraCtx.ServiceSchedules
                .Include(x => x.Service)
                .Where(x => x.IsActive == true && x.IsDeleted == false)
                .Where(x => x.Service.IsActive == true && x.IsDeleted == false)
                .Where(x => x.Service.EmployeeId == employeeId)
                .Where(x => x.StartDate > currentTime)
                .ToListAsync();

            if ((serviceSchedules?.Count ?? 0) == 0)
                return new List<ServiceDetails>();

            var employeeServices = serviceSchedules
                .Select(x => new ServiceDetails()
                {
                    ServiceId = x.Service.ServiceId,
                    ServiceName = x.Service.Title
                })
                .DistinctBy(x => x.ServiceId)
                .OrderBy(x => x.ServiceName)
                .ToList();

            return employeeServices;
        }

        public async Task<List<ServiceScheduleDetails>> GetAvailableServiceAppointmentsForBooking(int serviceId, DateTime date)
        {
            var today = DateTime.UtcNow.ToLocalTime();

            var scheduledAppointments = await _agendaLigeraCtx.Appointments
                .Include(x => x.ServiceRecipient)
                .Include(x => x.ServiceSchedule)
                .Where(x => x.IsActive == true && x.IsDeleted == false)
                .Where(x => x.ServiceSchedule.ServiceId == serviceId)
                .Where(x => x.ServiceSchedule.StartDate > today)
                .Where(x => x.ServiceSchedule.StartDate.Date == date.Date)
                .ToListAsync();

            var serviceSchedulesFound = await _agendaLigeraCtx.ServiceSchedules
                .Include(x => x.Service)
                .Where(x => x.IsActive == true && x.IsDeleted == false)
                .Where(x => x.Service.IsActive == true && x.IsDeleted == false)
                .Where(x => x.ServiceId == serviceId)
                .Where(x => x.StartDate > today)
                .Where(x => x.StartDate.Date == date.Date)
                .OrderBy(x => x.StartDate)
                .ToListAsync();

            var availableServiceSchedules = serviceSchedulesFound
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

        public async Task BookAppointment(int serviceRecipientId, int serviceScheduleId)
        {
            var appointment = new Appointment()
            {
                ServiceRecipientId = serviceRecipientId,
                ServiceScheduleId = serviceScheduleId,
                IsActive = true,
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow.ToLocalTime()
            };

            await _agendaLigeraCtx.AddAsync(appointment);
            await _agendaLigeraCtx.SaveChangesAsync();
        }
    }
}
