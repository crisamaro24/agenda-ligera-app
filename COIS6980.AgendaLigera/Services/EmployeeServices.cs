using COIS6980.EFCoreDb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace COIS6980.AgendaLigera.Services
{
    public interface IEmployeeServices
    {

    }
    public class EmployeeServices : IEmployeeServices
    {
        private readonly AgendaLigeraContext _agendaLigeraCtx;
        public EmployeeServices(AgendaLigeraContext agendaLigeraCtx)
        {
            _agendaLigeraCtx = agendaLigeraCtx;
        }
    }
}
