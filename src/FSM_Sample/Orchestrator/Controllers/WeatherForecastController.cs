using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;

namespace Orchestrator.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            //If there is no Exception thrown, there will have 15 lines
            //If there are at least 1 Exception thrown, there will have 5 lines

            var rng = new Random();
            var weatherForecast = Enumerable.Range(1, 5).Select(index => new WeatherForecast
                {
                    Date = DateTime.Now.AddDays(index),
                    TemperatureC = rng.Next(-20, 55),
                    Summary = Summaries[rng.Next(Summaries.Length)]
                });

            var fsm = new TransEnsuringMachine
            {
                Step1 = async () =>
                {
                    var serviceAWeatherForecast = await "http://aservice"
                        .AppendPathSegment("weatherforecast")
                        .GetJsonAsync<IEnumerable<WeatherForecast>>();
                    weatherForecast = weatherForecast.Concat(serviceAWeatherForecast);

                    if (rng.Next(1, 100) < 25)//Random through exception to test
                    {
                        throw new Exception();
                    }
                },
                RollbackStep1 = () =>
                {
                    //if there are error, remove the added elements
                    weatherForecast = weatherForecast.SkipLast(5);
                    return Task.CompletedTask;//No task to async => must return CompletedTask to avoid using async keywords
                },
                Step2 = async () =>
                {
                    var serviceBWeatherForecast = await "http://bservice"
                        .AppendPathSegment("weatherforecast")
                        .GetJsonAsync<IEnumerable<WeatherForecast>>();

                    weatherForecast = weatherForecast.Concat(serviceBWeatherForecast);

                    if (rng.Next(1, 100) < 75)//Random through exception to test
                    {
                        throw new Exception();
                    }
                },
                RollbackStep2 = () =>
                {
                    //if there are error, remove the added elements
                    weatherForecast = weatherForecast.SkipLast(5);
                    return Task.CompletedTask;
                }
            };

            await fsm.Start();
            
            return weatherForecast;
        }
    }
}
