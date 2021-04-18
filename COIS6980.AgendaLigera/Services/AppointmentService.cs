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
    }
}
