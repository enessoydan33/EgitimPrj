using EgitimPrj.Models.Response.Gamification;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace EgitimPrj.Controllers
{
    [Route("Gamification")]
    public class GamificationProxyController : ProxyControllerBase
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
            return await SendWithFallback(
                $"{ApiBase}/api/Achievements/my-achievements",
                $"{ApiBase}/api/Achievements",
                $"{ApiBase}/api/Achievement/my-achievements",
                $"{ApiBase}/api/Achievement"
            );
        }

        [HttpGet("AllAchievements")]
        public async Task<IActionResult> GetAllAchievements()
        {
            return await SendWithFallback(
                $"{ApiBase}/api/Achievements",
                $"{ApiBase}/api/Achievements/my-achievements",
                $"{ApiBase}/api/Achievement",
                $"{ApiBase}/api/Achievement/my-achievements"
            );
        }

        #endregion

        #region Streak

        [HttpGet("StreakStatus")]
        public async Task<IActionResult> GetStreakStatus()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/api/StudyStreaks/status");
            AttachAuth(request);
            return await SendProxyAsync(_http, request);
        }

        [HttpPost("CheckIn")]
        public async Task<IActionResult> CheckIn()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}/api/StudyStreaks/check-in");
            AttachAuth(request);
            return await SendProxyAsync(_http, request);
        }

        #endregion

        #region Helper Methods

        private async Task<IActionResult> SendWithFallback(params string[] urls)
        {
            HttpStatusCode lastStatus = HttpStatusCode.BadGateway;
            string lastContent = "No response";
            string lastContentType = "text/plain";

            foreach (var url in urls)
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, url);
                    AttachAuth(request);
                    var response = await _http.SendAsync(request);
                    var contentType = response.Content.Headers.ContentType?.MediaType ?? "text/plain";
                    var content = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        if (contentType.Contains("application/json"))
                        {
                            return new ContentResult
                            {
                                StatusCode = (int)response.StatusCode,
                                ContentType = "application/json",
                                Content = content
                            };
                        }

                        return Content(content, contentType);
                    }

                    lastStatus = response.StatusCode;
                    lastContent = content;
                    lastContentType = contentType;
                }
                catch (Exception ex)
                {
                    lastStatus = HttpStatusCode.InternalServerError;
                    lastContent = ex.Message;
                    lastContentType = "text/plain";
                }
            }

            if (lastContentType.Contains("application/json"))
            {
                return new ContentResult
                {
                    StatusCode = (int)lastStatus,
                    ContentType = "application/json",
                    Content = lastContent
                };
            }

            return StatusCode((int)lastStatus, new { message = lastContent });
        }

        #endregion
    }
}
