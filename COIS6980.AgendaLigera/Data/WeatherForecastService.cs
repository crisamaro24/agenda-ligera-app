using COIS6980.EFCoreDb.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace COIS6980.AgendaLigera.Data
{
    public interface IWeatherForecastService
    {
        Task<WeatherForecast[]> GetForecastAsync(DateTime startDate);
    }
    public class WeatherForecastService : IWeatherForecastService
    {
        private readonly AgendaLigeraContext _agendaLigeraCtx;
        private readonly ApplicationDbContext _authCtx;
        public WeatherForecastService(AgendaLigeraContext agendaLigeraCtx, ApplicationDbContext authCtx)
        {
            _agendaLigeraCtx = agendaLigeraCtx;
            _authCtx = authCtx;
        }

        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        public async Task<WeatherForecast[]> GetForecastAsync(DateTime startDate)
        {
            var employee = await _agendaLigeraCtx.Employees.FirstOrDefaultAsync(x => x.LastName == "Aponte");
            var userId = employee?.UserId ?? string.Empty;
            var user = await _authCtx.Users.FirstOrDefaultAsync(x => x.Id == userId);

            var rng = new Random();
            return await Task.FromResult(Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = startDate.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = user?.UserName ?? Summaries[rng.Next(Summaries.Length)]
            }).ToArray());
        }
    }
}
