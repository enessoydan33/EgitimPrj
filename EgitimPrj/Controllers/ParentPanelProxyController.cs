using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace EgitimPrj.Controllers
{
    [Route("[controller]")]
    public class ParentPanelProxyController : ProxyControllerBase
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _configuration;

        private string ParentPanelApiBase
        {
            get
            {
                var baseUrl = _configuration["ApiSettings:BaseUrl"]?.TrimEnd('/') ?? string.Empty;
                var path = _configuration["ApiPaths:HomeworkCheck"] ?? "/api/ParentPanel";
                return $"{baseUrl}{path}";
            }
        }

        public ParentPanelProxyController(HttpClient http, IConfiguration configuration)
        {
            _http = http;
            _configuration = configuration;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var denied = RequireSessionBearer("ParentToken", "Veli oturumunda token yok. Lütfen tekrar giriş yapın.");
            if (denied != null)
                return denied;

            var request = new HttpRequestMessage(HttpMethod.Get, $"{ParentPanelApiBase}/dashboard");
            AttachAuth(request, "ParentToken");
            return await SendProxyAsync(_http, request);
        }

        [HttpGet("messages")]
        public async Task<IActionResult> Messages()
        {
            var denied = RequireSessionBearer("ParentToken", "Veli oturumunda token yok. Lütfen tekrar giriş yapın.");
            if (denied != null)
                return denied;

            var request = new HttpRequestMessage(HttpMethod.Get, $"{ParentPanelApiBase}/messages");
            AttachAuth(request, "ParentToken");
            return await SendProxyAsync(_http, request);
        }

        [HttpGet("trial-exams/{id:int}")]
        public async Task<IActionResult> TrialExamDetail(int id)
        {
            var denied = RequireSessionBearer("ParentToken", "Veli oturumunda token yok. Lütfen tekrar giriş yapın.");
            if (denied != null)
                return denied;

            var request = new HttpRequestMessage(HttpMethod.Get, $"{ParentPanelApiBase}/trial-exams/{id}");
            AttachAuth(request, "ParentToken");
            return await SendProxyAsync(_http, request);
        }

        /// <summary>
        /// Bağlı öğrencinin haftalık etüt programı. API veli JWT ile Pomodoro uç noktasını desteklemelidir.
        /// </summary>
        [HttpGet("schedule/weekly")]
        public async Task<IActionResult> WeeklyStudentSchedule()
        {
            var denied = RequireSessionBearer("ParentToken", "Veli oturumunda token yok. Lütfen tekrar giriş yapın.");
            if (denied != null)
                return denied;

            var baseUrl = _configuration["ApiSettings:BaseUrl"]?.TrimEnd('/') ?? string.Empty;
            var path = _configuration["ApiPaths:ParentStudentWeeklySchedule"] ?? "/api/Pomodoro/schedule/weekly";
            if (!path.StartsWith('/'))
                path = "/" + path;
            var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}{path}");
            AttachAuth(request, "ParentToken");
            return await SendProxyAsync(_http, request);
        }

        [HttpGet("appointments/teachers")]
        public async Task<IActionResult> AppointmentTeachers()
        {
            var denied = RequireSessionBearer("ParentToken", "Veli oturumunda token yok. Lütfen tekrar giriş yapın.");
            if (denied != null)
                return denied;

            var request = new HttpRequestMessage(HttpMethod.Get, $"{ParentPanelApiBase}/appointments/teachers");
            AttachAuth(request, "ParentToken");
            return await SendProxyAsync(_http, request);
        }

        [HttpPost("appointments/request")]
        public async Task<IActionResult> RequestAppointment([FromBody] System.Text.Json.JsonElement body)
        {
            var denied = RequireSessionBearer("ParentToken", "Veli oturumunda token yok. Lütfen tekrar giriş yapın.");
            if (denied != null)
                return denied;

            var jsonString = System.Text.Json.JsonSerializer.Serialize(body);
            var request = new HttpRequestMessage(HttpMethod.Post, $"{ParentPanelApiBase}/appointments/request")
            {
                Content = new StringContent(jsonString, Encoding.UTF8, "application/json")
            };
            AttachAuth(request, "ParentToken");
            return await SendProxyAsync(_http, request);
        }
    }
}

