using EgitimPrj.Models.Request;
using EgitimPrj.Models.Response;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace EgitimPrj.Controllers
{
    public class PomodoroController : Controller
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _configuration;
        // CoMentor.API Pomodoro endpoint'i
        private string ApiBase => $"{_configuration["ApiSettings:BaseUrl"]}/api/Pomodoro";

        public PomodoroController(HttpClient http, IConfiguration configuration)
        {
            _http = http;
            _configuration = configuration;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(StartPomodoroRequest model)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}/start")
            {
                Content = JsonContent.Create(model)
            };
            AttachAuth(request);
            return await Send(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int sessionId, CompletePomodoroRequest? model)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}/{sessionId}/complete")
            {
                Content = JsonContent.Create(model ?? new CompletePomodoroRequest())
            };
            AttachAuth(request);
            return await Send(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int sessionId)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}/{sessionId}/cancel");
            AttachAuth(request);
            return await Send(request);
        }

        [HttpGet]
        public async Task<IActionResult> Active()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/active");
            AttachAuth(request);
            return await Send(request);
        }

        [HttpGet]
        public async Task<IActionResult> History(int? subjectId = null, int limit = 50)
        {
            var url = $"{ApiBase}/history?limit={limit}";
            if (subjectId.HasValue)
                url += $"&subjectId={subjectId.Value}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            AttachAuth(request);
            return await Send(request);
        }

        [HttpGet]
        public async Task<IActionResult> Stats()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/stats");
            AttachAuth(request);
            return await Send(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSchedule([FromBody] CreateScheduleRequest model)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}/schedule")
            {
                Content = JsonContent.Create(model)
            };
            AttachAuth(request);
            return await Send(request);
        }

        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSchedule(int scheduleId, [FromBody] UpdateScheduleRequest model)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, $"{ApiBase}/schedule/{scheduleId}")
            {
                Content = JsonContent.Create(model)
            };
            AttachAuth(request);
            return await Send(request);
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSchedule(int scheduleId)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, $"{ApiBase}/schedule/{scheduleId}");
            AttachAuth(request);
            return await Send(request);
        }

        [HttpGet]
        public async Task<IActionResult> WeeklySchedule()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/schedule/weekly");
            AttachAuth(request);
            return await Send(request);
        }

        [HttpGet]
        public async Task<IActionResult> TodaySchedule()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/schedule/today");
            AttachAuth(request);
            return await Send(request);
        }

        [HttpGet]
        public async Task<IActionResult> RandomVideoRecommendation()
        {
            var baseUrl = _configuration["ApiSettings:BaseUrl"]?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, "API base URL tanımlı değil.");
            }

            var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/api/VideoRecommendation/random");
            AttachAuth(request);
            return await Send(request);
        }

        private async Task<IActionResult> Send(HttpRequestMessage request)
        {
            try
            {
                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var contentType = response.Content.Headers.ContentType?.MediaType;
                    if (contentType != null && contentType.Contains("application/json"))
                    {
                        var json = await response.Content.ReadFromJsonAsync<object>();
                        return Json(json);
                    }

                    var text = await response.Content.ReadAsStringAsync();
                    return Content(text, contentType ?? "text/plain");
                }

                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, $"API hatası: {ex.Message}");
            }
        }

        private void AttachAuth(HttpRequestMessage requestMessage)
        {
            var token = HttpContext.Session.GetString("Token");
            if (!string.IsNullOrWhiteSpace(token))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            // Ngrok tarayıcı uyarısını atlamak için gerekli header
            requestMessage.Headers.Add("ngrok-skip-browser-warning", "true");
        }
    }
}

