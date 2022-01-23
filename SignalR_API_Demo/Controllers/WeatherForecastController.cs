using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SignalR_API_Demo.Extention;
using SignalR_API_Demo.Hubs;
using SignalR_API_Demo.Model;

namespace SignalR_API_Demo.Controllers
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
        public IHubContext<StronglyTypeNotificationHub, INotificationClient> _strongNotificationHubContext { get; }
        public IHubContext<StronglyTypeNotificationHub> _notificationHub { get; }

        public WeatherForecastController(ILogger<WeatherForecastController> logger
                , IHubContext<StronglyTypeNotificationHub, INotificationClient> strongNotificationHubContext
                , IHubContext<StronglyTypeNotificationHub> notificationHub)
        {
            _logger = logger;
            _strongNotificationHubContext = strongNotificationHubContext;
            _notificationHub = notificationHub;
        }

        [HttpGet]
        [AuthorizeUser(Method = "signalr", Actions = new[] { Actions.get })]
        public async Task<IEnumerable<WeatherForecast>> SendToGlobalId([FromQuery] string globalId)
        {
            await _strongNotificationHubContext.Clients
                .User(globalId)
                .ReceiveMessage($"Notify:{globalId} Weather forecast accessed: {DateTime.Now}");

            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpPost]
        [AuthorizeUser(Method = "signalr", Actions = new[] { Actions.get })]
        public async Task<IEnumerable<Subscriber>> SendToGlobalIds([FromBody] List<string> globalIds)
        {
            await _strongNotificationHubContext.Clients
                .Users(globalIds)
                .ReceiveMessage($"Notify:{globalIds.Count} Global Id/s Weather forecast accessed: {DateTime.Now}");

            var serverMessage = new ServerMessage()
            {
                DateTime = DateTimeOffset.Now.ToString(),
                Message = $"Notify:{globalIds.Count} Global Id/s This is object: {DateTime.Now}"
            };

            await _strongNotificationHubContext.Clients
                .Users(globalIds)
                .ReceiveMessageObject(serverMessage);

            var x = new StronglyTypeNotificationHub();

            return x.GetAllSubscribers();
        }
    }
}
