using COIS6980.EFCoreDb.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace COIS6980.AgendaLigera.Services
{
    public interface IReportService
    {
        Task<DataTable> GetYTDCustomerData(string userId);
        Task<DataTable> GetYTDAppointmentData(string userId);

    }
    public class ReportService : IReportService
    {
        private readonly AgendaLigeraContext _agendaLigeraCtx;
        public ReportService(AgendaLigeraContext agendaLigeraCtx)
        {
            _agendaLigeraCtx = agendaLigeraCtx;
        }

        public async Task<DataTable> GetYTDCustomerData(string userId)
        {
            var currentYear = DateTime.UtcNow.ToLocalTime().Year;
            var startOfCurrentYear = new DateTime(currentYear, 1, 1);
            var today = DateTime.UtcNow.ToLocalTime();

            var customers = await _agendaLigeraCtx.ServiceRecipients
                .Include(x => x.User)
                .Where(x => x.IsDeleted == false)
                .Where(x => x.CreatedDate.Date >= startOfCurrentYear.Date)
                .Where(x => x.CreatedDate.Date <= today.Date)
                .Where(x => x.UserId == userId)
                .ToListAsync();

            var customersDataTable = new DataTable();
            customersDataTable.Columns.Add("Paciente");
            customersDataTable.Columns.Add("Correo Electrónico");
            customersDataTable.Columns.Add("Fecha de Ingreso");
            customersDataTable.Columns.Add("Estado");

            DataRow customersDataRow;
            foreach (var customer in customers)
            {
                customersDataRow = customersDataTable.NewRow();
                customersDataRow["Paciente"] = customer.FirstName + " " + customer.LastName;
                customersDataRow["Correo Electrónico"] = customer.User.Email;
                customersDataRow["Fecha de Ingreso"] = customer.CreatedDate.ToString("MMMM/dd/yyyy", new CultureInfo("es-ES")); ;
                customersDataRow["Estado"] = (customer.IsActive ?? false) ? "Activo" : "Inactivo";

                customersDataTable.Rows.Add(customersDataRow);
            }

            return customersDataTable;
        }

        public async Task<DataTable> GetYTDAppointmentData(string userId)
        {
            var currentYear = DateTime.UtcNow.ToLocalTime().Year;
            var startOfCurrentYear = new DateTime(currentYear, 1, 1);
            var today = DateTime.UtcNow.ToLocalTime();

            var appointments =
                await _agendaLigeraCtx.Appointments
                .Include(x => x.ServiceRecipient)
                    .ThenInclude(x => x.User)
                .Include(x => x.ServiceSchedule)
                    .ThenInclude(x => x.Service)
                    .ThenInclude(x => x.Employee)
                .Where(x => x.IsDeleted == false)
                .Where(x => x.ServiceSchedule.StartDate.Date >= startOfCurrentYear.Date)
                .Where(x => x.ServiceSchedule.EndDate.Date <= today.Date)
                .Where(x => x.ServiceSchedule.Service.Employee.UserId == userId)
                .ToListAsync();

            var appointmentsDataTable = new DataTable();
            appointmentsDataTable.Columns.Add("Fecha");
            appointmentsDataTable.Columns.Add("Hora");
            appointmentsDataTable.Columns.Add("Servicio");
            appointmentsDataTable.Columns.Add("Paciente");
            appointmentsDataTable.Columns.Add("Cancelada");

            DataRow appointmentsDataRow;
            foreach (var appointment in appointments)
            {
                appointmentsDataRow = appointmentsDataTable.NewRow();
                appointmentsDataRow["Fecha"] = appointment.ServiceSchedule.StartDate.ToString("MMMM/dd/yyyy", new CultureInfo("es-ES"));
                appointmentsDataRow["Hora"] = appointment.ServiceSchedule.StartDate.ToString("hh:mm tt") + " - " + appointment.ServiceSchedule.EndDate.ToString("hh:mm tt");
                appointmentsDataRow["Servicio"] = appointment.ServiceSchedule.Service.Title;
                appointmentsDataRow["Paciente"] = appointment.ServiceRecipient.FirstName + " " + appointment.ServiceRecipient.LastName;
                appointmentsDataRow["Cancelada"] = (appointment.IsActive ?? false) ? "No" : "Sí";

                appointmentsDataTable.Rows.Add(appointmentsDataRow);
            }

            return appointmentsDataTable;
        }
    }
}
