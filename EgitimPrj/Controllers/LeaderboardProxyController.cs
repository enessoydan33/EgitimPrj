using EgitimPrj.Models.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace EgitimPrj.Controllers
{
    [Route("LeaderboardProxy")]
    public class LeaderboardProxyController : ProxyControllerBase
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _configuration;
        // CoMentor.API Leaderboard endpoint'i
        private string ApiBase => $"{_configuration["ApiSettings:BaseUrl"]}/api/Leaderboard";

        public LeaderboardProxyController(HttpClient http, IConfiguration configuration)
        {
            _http = http;
            _configuration = configuration;
        }

        /// <summary>
        /// Genel XP sıralamasını getirir (En yüksek XP'den düşüğe)
        /// </summary>
        [HttpGet("GetGeneralLeaderboard")]
        public async Task<IActionResult> GetGeneralLeaderboard([FromQuery] int limit = 100)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}?limit={limit}");
            // Genel leaderboard AllowAnonymous, auth gerekmiyor ama ngrok header'ı gerekli
            request.Headers.Add("ngrok-skip-browser-warning", "true");
            return await SendProxyAsync(_http, request);
        }

        /// <summary>
        /// Okul ligi sıralamasını getirir (aynı okuldaki kullanıcılar)
        /// </summary>
        [HttpGet("school")]
        public async Task<IActionResult> GetSchoolLeaderboard([FromQuery] int limit = 100)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/school?limit={limit}");
            AttachAuth(request);
            return await SendProxyAsync(_http, request);
        }

        /// <summary>
        /// Sınıf ligi sıralamasını getirir (aynı sınıftaki kullanıcılar)
        /// </summary>
        [HttpGet("grade")]
        public async Task<IActionResult> GetGradeLeaderboard([FromQuery] int limit = 100)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/grade?limit={limit}");
            AttachAuth(request);
            return await SendProxyAsync(_http, request);
        }

        /// <summary>
        /// Tüm ligleri tek seferde getirir (Genel, Okul, Sınıf)
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllLeagues([FromQuery] int limit = 50)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/all?limit={limit}");
            AttachAuth(request);
            return await SendProxyAsync(_http, request);
        }

        /// <summary>
        /// Kullanıcının XP geçmişini getirir
        /// </summary>
        [HttpGet("GetXpHistory")]
        public async Task<IActionResult> GetXpHistory([FromQuery] int limit = 50)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/xp/history?limit={limit}");
            AttachAuth(request);
            return await SendProxyAsync(_http, request);
        }

        /// <summary>
        /// Kullanıcının XP özetini getirir (toplam, bugün, hafta, sıralamalar)
        /// </summary>
        [HttpGet("GetXpSummary")]
        public async Task<IActionResult> GetXpSummary()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/xp/summary");
            AttachAuth(request);
            return await SendProxyAsync(_http, request);
        }
    }
}
