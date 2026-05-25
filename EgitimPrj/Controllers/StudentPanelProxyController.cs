using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace EgitimPrj.Controllers
{
    [Route("[controller]")]
    public class StudentPanelProxyController : ProxyControllerBase
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _configuration;

        private string StudentPanelBase => $"{_configuration["ApiSettings:BaseUrl"]}/api/StudentPanel";

        public StudentPanelProxyController(HttpClient http, IConfiguration configuration)
        {
            _http = http;
            _configuration = configuration;
        }

        [HttpGet("announcements")]
        public async Task<IActionResult> Announcements()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{StudentPanelBase}/announcements");
            AttachAuth(request);
            return await SendProxyAsync(_http, request);
        }

        [HttpGet("homeworks")]
        public async Task<IActionResult> Homeworks()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{StudentPanelBase}/homeworks");
            AttachAuth(request);
            return await SendProxyAsync(_http, request);
        }

        [HttpGet("appointments")]
        public async Task<IActionResult> Appointments()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{StudentPanelBase}/appointments");
            AttachAuth(request);
            return await SendProxyAsync(_http, request);
        }

        [HttpGet("appointments/teachers")]
        public async Task<IActionResult> AppointmentTeachers()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{StudentPanelBase}/appointments/teachers");
            AttachAuth(request);
            return await SendProxyAsync(_http, request);
        }

        [HttpPost("appointments/request")]
        public async Task<IActionResult> RequestAppointment([FromBody] System.Text.Json.JsonElement body)
        {
            var jsonString = System.Text.Json.JsonSerializer.Serialize(body);
            var request = new HttpRequestMessage(HttpMethod.Post, $"{StudentPanelBase}/appointments/request")
            {
                Content = new StringContent(jsonString, Encoding.UTF8, "application/json")
            };
            AttachAuth(request);
            return await SendProxyAsync(_http, request);
        }
    }
}
