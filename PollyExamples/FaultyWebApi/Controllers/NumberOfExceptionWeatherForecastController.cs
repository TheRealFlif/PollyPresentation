using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FaultyWebApi.Controllers
{
    using System.Diagnostics.Tracing;
    using System.Threading;

    [ApiController]
    [Route("[controller]")]
    public class NumberOfExceptionWeatherForecastController : ControllerBase
    {
        private static int _counter = 1;
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<NumberOfExceptionWeatherForecastController> _logger;

        public NumberOfExceptionWeatherForecastController(ILogger<NumberOfExceptionWeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("{number}")]
        public IEnumerable<WeatherForecast> Get(int number)
        {
            _counter++;

            if (_counter < number)
            {
                throw new ApplicationException($"Sometimes wrong, sometimes right, {number} of {_counter}");
            }

            _counter = 0;
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
                {
                    Date = DateTime.Now.AddDays(index),
                    TemperatureC = rng.Next(-20, 55),
                    Summary = Summaries[rng.Next(Summaries.Length)]
                })
                .ToArray();
        }
    }
}