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
    public class InternalServerErrorWeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<InternalServerErrorWeatherForecastController> _logger;

        public InternalServerErrorWeatherForecastController(ILogger<InternalServerErrorWeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            throw new ApplicationException("Always wrong, never right");
        }
    }
}