using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FaultyWebApi.Controllers
{
    using System.Threading;

    [ApiController]
    [Route("[controller]")]
    public class SlowWeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<SlowWeatherForecastController> _logger;

        public SlowWeatherForecastController(ILogger<SlowWeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            return Get(1);
        }

        [HttpGet]
        [Route("{number}")]
        public IEnumerable<WeatherForecast> Get(int number)
        {
            var rng = new Random();
            Thread.Sleep(number*1000);
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