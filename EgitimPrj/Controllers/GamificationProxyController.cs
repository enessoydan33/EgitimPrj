using EgitimPrj.Models.Response.Gamification;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Headers;

namespace EgitimPrj.Controllers
{
    [Route("Gamification")]
    public class GamificationProxyController : Controller
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _configuration;
        private string ApiBase => _configuration["ApiSettings:BaseUrl"];

        public GamificationProxyController(HttpClient http, IConfiguration configuration)
        {
            _http = http;
            _configuration = configuration;
        }

        #region Achievements

        [HttpGet("MyAchievements")]
        public async Task<IActionResult> GetMyAchievements()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/api/Achievements/my-achievements");
            AttachAuth(request);
            return await Send(request);
        }

        [HttpGet("AllAchievements")]
        public async Task<IActionResult> GetAllAchievements()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/api/Achievements");
            AttachAuth(request);
            return await Send(request);
        }

        #endregion

        #region Streak

        [HttpGet("StreakStatus")]
        public async Task<IActionResult> GetStreakStatus()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/api/StudyStreaks/status");
            AttachAuth(request);
            return await Send(request);
        }

        [HttpPost("CheckIn")]
        public async Task<IActionResult> CheckIn()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}/api/StudyStreaks/check-in");
            AttachAuth(request);
            return await Send(request);
        }

        #endregion

        #region Helper Methods

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

                try
                {
                    var errorJson = await response.Content.ReadFromJsonAsync<object>();
                    return StatusCode((int)response.StatusCode, errorJson);
                }
                catch
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, new { message = errorContent });
                }
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new { message = $"API hatası: {ex.Message}" });
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

        #endregion
    }
}
