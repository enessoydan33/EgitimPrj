using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;

namespace EgitimPrj.Controllers
{
    [Route("[controller]")]
    public class StudentPanelProxyController : Controller
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
            AttachUserAuth(request);
            return await Send(request);
        }

        [HttpGet("homeworks")]
        public async Task<IActionResult> Homeworks()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{StudentPanelBase}/homeworks");
            AttachUserAuth(request);
            return await Send(request);
        }

        private async Task<IActionResult> Send(HttpRequestMessage request)
        {
            try
            {
                var response = await _http.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var contentType = response.Content.Headers.ContentType?.MediaType;
                    if (contentType != null && contentType.Contains("application/json"))
                    {
                        return Content(body, "application/json");
                    }

                    return Content(body, contentType ?? "text/plain");
                }

                Console.WriteLine("API ERROR: " + response.StatusCode + " - " + body);
                return StatusCode((int)response.StatusCode, new { error = true, details = body });
            }
            catch (Exception ex)
            {
                Console.WriteLine("PROXY EXCEPTION: " + ex.ToString());
                return StatusCode((int)HttpStatusCode.InternalServerError, new { error = true, details = ex.Message });
            }
        }

        private void AttachUserAuth(HttpRequestMessage request)
        {
            // Öğrenci JWT token'ı
            var token = HttpContext.Session.GetString("Token");
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            request.Headers.Add("ngrok-skip-browser-warning", "true");
        }
    }
}
