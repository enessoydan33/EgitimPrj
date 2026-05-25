using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace EgitimPrj.Controllers
{
    [Route("MlPrediction")]
    public class MlPredictionProxyController : ProxyControllerBase
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
            return await SendProxyAsync(_http, request);
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
            return await SendProxyAsync(_http, request);
        }
    }
}
