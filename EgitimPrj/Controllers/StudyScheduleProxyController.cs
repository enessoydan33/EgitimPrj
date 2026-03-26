using EgitimPrj.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace EgitimPrj.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudyScheduleProxyController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        public StudyScheduleProxyController(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient("ApiClient");
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        /// <summary>
        /// AI ile program oluşturur (CoMentor API).
        /// </summary>
        [HttpPost("Generate")]
        public async Task<IActionResult> GenerateSchedule([FromBody] GenerateScheduleRequestDto request)
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("Token");
            if (string.IsNullOrEmpty(token))
                return Unauthorized("Token bulunamadı. Lütfen tekrar giriş yapın.");

            var baseUrl = _configuration["ApiSettings:BaseUrl"];
            var requestMessage = BuildRequest(HttpMethod.Post, $"{baseUrl}/api/StudySchedule/generate", token, request);
            var response = await _httpClient.SendAsync(requestMessage);

            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<List<StudyScheduleDto>>(responseData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return Ok(result);
            }

            var errorData = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, $"API Error: {errorData}");
        }

        /// <summary>
        /// AI ile program oluşturur ve her etüdü Pomodoro API'sine de kaydeder.
        /// </summary>
        [HttpPost("GenerateAndSave")]
        public async Task<IActionResult> GenerateAndSave([FromBody] GenerateScheduleRequestDto request)
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("Token");
            if (string.IsNullOrEmpty(token))
                return Unauthorized("Token bulunamadı. Lütfen tekrar giriş yapın.");

            var baseUrl = _configuration["ApiSettings:BaseUrl"];

            // 1. AI'dan program al
            var genRequest = BuildRequest(HttpMethod.Post, $"{baseUrl}/api/StudySchedule/generate", token, request);
            var genResponse = await _httpClient.SendAsync(genRequest);

            if (!genResponse.IsSuccessStatusCode)
            {
                var errData = await genResponse.Content.ReadAsStringAsync();
                return StatusCode((int)genResponse.StatusCode, $"AI Error: {errData}");
            }

            var jsonStr = await genResponse.Content.ReadAsStringAsync();
            var scheduleItems = JsonSerializer.Deserialize<List<StudyScheduleDto>>(jsonStr, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (scheduleItems == null || scheduleItems.Count == 0)
                return Ok(new { saved = 0, items = new List<StudyScheduleDto>() });

            // 2. Her etüdü Pomodoro API'ye kaydet
            int saved = 0;
            foreach (var item in scheduleItems)
            {
                var pomPayload = new
                {
                    SubjectId = item.SubjectId,
                    DayOfWeek = item.DayOfWeek,
                    StartTime = item.StartTime,
                    EndTime = item.EndTime,
                    Topic = item.Topic ?? ""
                };

                var pomRequest = BuildRequest(HttpMethod.Post, $"{baseUrl}/api/Pomodoro/schedule", token, pomPayload);
                var pomResponse = await _httpClient.SendAsync(pomRequest);
                if (pomResponse.IsSuccessStatusCode) saved++;
            }

            return Ok(new { saved, items = scheduleItems });
        }

        // ---- YARDIMCI ----
        private HttpRequestMessage BuildRequest(HttpMethod method, string url, string token, object? body = null)
        {
            var msg = new HttpRequestMessage(method, url);
            msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            msg.Headers.Add("ngrok-skip-browser-warning", "true");
            if (body != null)
            {
                var json = JsonSerializer.Serialize(body);
                msg.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }
            return msg;
        }
    }
}
