using COIS6980.AgendaLigera.Models.Service;
using COIS6980.EFCoreDb.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static COIS6980.AgendaLigera.Models.Enums.AllEnumerations;

namespace COIS6980.AgendaLigera.Services
{
    public interface IEmployeeServices
    {
        Task<List<EmployeeService>> GetServicesOffered(string userId = null, bool active = true, bool deleted = false);
        Task<List<Models.Service.ServiceSchedule>> GetServiceSchedulesBetweenDates(
            int serviceId,
            DateTime startDate,
            DateTime endDate,
            string userId = null,
            bool active = true,
            bool deleted = false);
        Task<bool> DuplicateServiceFound(string userId, string serviceName);
        Task CreateService(string userId, string serviceName, string serviceDescription, int estimatedDurationInMinutes);
        Task AddServiceSchedule(
            int serviceId,
            int capacity,
            DateTime startDate,
            DateTime startTime,
            DateTime endTime,
            RecurrenceOptionEnum recurrenceOption,
            DateTime endDate);
    }
    public class EmployeeServices : IEmployeeServices
    {
        private readonly AgendaLigeraContext _agendaLigeraCtx;
        public EmployeeServices(AgendaLigeraContext agendaLigeraCtx)
        {
            _agendaLigeraCtx = agendaLigeraCtx;
        }

        public async Task<List<EmployeeService>> GetServicesOffered(string userId = null, bool active = true, bool deleted = false)
        {
            var servicesQuery = _agendaLigeraCtx.Services
                .Include(x => x.Employee)
                .Where(x => x.IsActive == active && x.IsDeleted == deleted);

            if (!string.IsNullOrWhiteSpace(userId))
                servicesQuery = servicesQuery
                    .Where(x => x.Employee.UserId == userId);

            var servicesFound = await servicesQuery.ToListAsync();

            if ((servicesFound?.Count ?? 0) == 0)
                return new List<EmployeeService>();

            var servicesOffered = servicesFound
                .Select(x => new EmployeeService()
                {
                    ServiceId = x.ServiceId,
                    ServiceName = x.Title,
                    ServiceDescription = x.Description,
                    EstimatedDurationInMinutes = x.EstimatedDurationInMinutes.GetValueOrDefault(),
                    IsActive = x.IsActive.GetValueOrDefault()
                })
                .OrderBy(x => x.ServiceName)
                .ToList();

            return servicesOffered;
        }

        public async Task<List<Models.Service.ServiceSchedule>> GetServiceSchedulesBetweenDates(
            int serviceId,
            DateTime startDate,
            DateTime endDate,
            string userId = null,
            bool active = true,
            bool deleted = false)
        {
            if (startDate > endDate)
                return new List<Models.Service.ServiceSchedule>();

            var serviceSchedulesQuery = _agendaLigeraCtx.ServiceSchedules
                .Include(x => x.Service)
                    .ThenInclude(x => x.Employee)
                .Where(x => x.ServiceId == serviceId)
                .Where(x => x.IsActive == active && x.IsDeleted == deleted)
                .Where(x => x.StartDate.Date >= startDate.Date)
                .Where(x => x.EndDate.Date <= endDate.Date);

            if (!string.IsNullOrWhiteSpace(userId))
                serviceSchedulesQuery = serviceSchedulesQuery
                    .Where(x => x.Service.Employee.UserId == userId);

            var serviceSchedulesFound = await serviceSchedulesQuery.ToListAsync();

            if ((serviceSchedulesFound?.Count ?? 0) == 0)
                return new List<Models.Service.ServiceSchedule>();

            var scheduledAppointments = await _agendaLigeraCtx.Appointments
                .Include(x => x.ServiceSchedule)
                .Where(x => x.IsActive == true && x.IsDeleted == false)
                .Where(x => x.ServiceSchedule.ServiceId == serviceId)
                .Where(x => x.ServiceSchedule.StartDate.Date >= startDate.Date)
                .Where(x => x.ServiceSchedule.EndDate.Date <= endDate.Date)
                .ToListAsync();

            var serviceSchedules = serviceSchedulesFound
                .Select(x => new Models.Service.ServiceSchedule()
                {
                    ServiceScheduleId = x.ServiceScheduleId,
                    ScheduleDisplayText = x.StartDate.ToString("hh:mm tt") +
                        " (" + scheduledAppointments.Where(y => y.ServiceScheduleId == x.ServiceScheduleId).Count() + ")",
                    Start = x.StartDate,
                    End = x.EndDate,
                    Capacity = x.Capacity?.ToString() ?? "Indefinido",
                    ScheduledCount = scheduledAppointments.Where(y => y.ServiceScheduleId == x.ServiceScheduleId).Count()
                })
                .OrderBy(x => x.Start)
                .ToList();

            return serviceSchedules;
        }

        public async Task<bool> DuplicateServiceFound(string userId, string serviceName)
        {
            var employeeService = await _agendaLigeraCtx.Services
                .Include(x => x.Employee)
                .Where(x => x.IsActive == true && x.IsDeleted == false)
                .Where(x => x.Employee.UserId == userId)
                .Where(x => x.Title == serviceName)
                .FirstOrDefaultAsync();

            return (employeeService?.ServiceId ?? 0) != 0;
        }

        public async Task CreateService(string userId, string serviceName, string serviceDescription, int estimatedDurationInMinutes)
        {
            var employee = await _agendaLigeraCtx.Employees
                .Where(x => x.IsActive == true && x.IsDeleted == false)
                .Where(x => x.UserId == userId)
                .FirstOrDefaultAsync();

            if ((employee?.EmployeeId ?? 0) != 0)
            {
                var service = new Service()
                {
                    EmployeeId = employee.EmployeeId,
                    Title = serviceName,
                    Description = serviceDescription,
                    EstimatedDurationInMinutes = estimatedDurationInMinutes,
                    IsActive = true,
                    IsDeleted = false
                };

                await _agendaLigeraCtx.AddAsync(service);
                await _agendaLigeraCtx.SaveChangesAsync();
            }
        }

        public async Task AddServiceSchedule(
            int serviceId,
            int capacity,
            DateTime startDate,
            DateTime startTime,
            DateTime endTime,
            RecurrenceOptionEnum recurrenceOption,
            DateTime endDate)
        {

        }
    }
}
