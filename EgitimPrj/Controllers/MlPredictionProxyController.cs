using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace EgitimPrj.Controllers
{
    [Route("MlPrediction")]
    public class MlPredictionProxyController : Controller
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _configuration;

        // CoMentor.API üzerinden Python ML servisine köprü kurar
        private string MlApiBase => $"{_configuration["ApiSettings:BaseUrl"]}/api/MlPrediction";

        public MlPredictionProxyController(HttpClient http, IConfiguration configuration)
        {
            _http = http;
            _configuration = configuration;
        }

        /// <summary>
        /// AYT Sayısal deneme netlerine göre sınav tahmini yapar.
        /// Body: { "denemeler": { "1": {"matematik":30, "fen":25} }, "sinav_tarihi": "21.06.2025" }
        /// </summary>
        [HttpPost("SayisalTahmin")]
        public async Task<IActionResult> SayisalTahmin([FromBody] object requestBody)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{MlApiBase}/sayisal-tahmin")
            {
                Content = JsonContent.Create(requestBody)
            };
            AttachAuth(request);
            return await Send(request);
        }

        /// <summary>
        /// TYT deneme netlerine göre sınav tahmini yapar.
        /// Body: { "denemeler": { "1": {"turkce":28, "matematik":20, "fen":12, "sosyal":15} }, "sinav_tarihi": "21.06.2025" }
        /// </summary>
        [HttpPost("TytTahmin")]
        public async Task<IActionResult> TytTahmin([FromBody] object requestBody)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{MlApiBase}/tyt-tahmin")
            {
                Content = JsonContent.Create(requestBody)
            };
            AttachAuth(request);
            return await Send(request);
        }

        #region Private Helpers

        private async Task<IActionResult> Send(HttpRequestMessage request)
        {
            try
            {
                var response = await _http.SendAsync(request);

                var rawJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Content(rawJson, "application/json");
                }

                return StatusCode((int)response.StatusCode, new { message = rawJson });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"ML API hatası: {ex.Message}" });
            }
        }

        private void AttachAuth(HttpRequestMessage requestMessage)
        {
            var token = HttpContext.Session.GetString("Token");
            if (!string.IsNullOrWhiteSpace(token))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            requestMessage.Headers.Add("ngrok-skip-browser-warning", "true");
        }

        #endregion
    }
}
