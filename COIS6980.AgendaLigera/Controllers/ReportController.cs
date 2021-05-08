using AspNetCore.Reporting;
using COIS6980.AgendaLigera.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace COIS6980.AgendaLigera.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ReportController(IReportService reportService, IWebHostEnvironment webHostEnvironment)
        {
            _reportService = reportService;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        [Route("GetCustomerReport/{userId}/{reportType}")]
        public async Task<IActionResult> GetYTDCustomerReport([FromRoute] string userId, [FromRoute] int reportType)
        {
            var customersDataTable = await _reportService.GetYTDCustomerData(userId);
            var reportPath = $"{_webHostEnvironment.WebRootPath}\\Reports\\CustomerReport.rdlc";

            LocalReport localReport = new LocalReport(reportPath);
            localReport.AddDataSource("dsCustomerYTD", customersDataTable);

            ReportResult result;

            if (reportType == 1)
            {
                result = localReport.Execute(RenderType.Pdf);
                return File(result.MainStream, "application/pdf");
            }
            else
            {
                result = localReport.Execute(RenderType.ExcelOpenXml);
                return File(result.MainStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            }
        }

        [HttpGet]
        [Route("GetAppointmentReport/{userId}/{reportType}")]
        public async Task<IActionResult> GetYTDAppointmentReport([FromRoute] string userId, [FromRoute] int reportType)
        {
            var appointmentsDataTable = await _reportService.GetYTDAppointmentData(userId);
            var reportPath = $"{_webHostEnvironment.WebRootPath}\\Reports\\AppointmentReport.rdlc";

            LocalReport localReport = new LocalReport(reportPath);
            localReport.AddDataSource("dsAppointmentYTD", appointmentsDataTable);

            ReportResult result;

            if (reportType == 1)
            {
                result = localReport.Execute(RenderType.Pdf);
                return File(result.MainStream, "application/pdf");
            }
            else
            {
                result = localReport.Execute(RenderType.ExcelOpenXml);
                return File(result.MainStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            }
        }
    }
}
