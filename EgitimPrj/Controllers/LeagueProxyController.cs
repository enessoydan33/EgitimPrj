using EgitimPrj.Models.Response;
using Microsoft.AspNetCore.Mvc;

namespace EgitimPrj.Controllers
{
    [Route("League")]
    public class LeagueProxyController : ProxyControllerBase
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _configuration;
        private string ApiBase => $"{_configuration["ApiSettings:BaseUrl"]}/api/League";

        public LeagueProxyController(HttpClient http, IConfiguration configuration)
        {
            _http = http;
            _configuration = configuration;
        }

        #region Lig Bilgileri

        /// <summary>
        /// Tüm ligleri getirir (Bronz, Gümüş, Altın, Platin, Elmas)
        /// </summary>
        [HttpGet("All")]
        public async Task<IActionResult> GetAllLeagues()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, ApiBase);
            request.Headers.Add("ngrok-skip-browser-warning", "true");
            return await SendProxyAsync(_http, request);
        }

        /// <summary>
        /// Belirli bir ligi getirir
        /// </summary>
        [HttpGet("{leagueId}")]
        public async Task<IActionResult> GetLeagueById(int leagueId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/{leagueId}");
            request.Headers.Add("ngrok-skip-browser-warning", "true");
            return await SendProxyAsync(_http, request);
        }

        /// <summary>
        /// Tüm liglerin genel görünümünü getirir (kullanıcı sayılarıyla birlikte)
        /// </summary>
        [HttpGet("Overview")]
        public async Task<IActionResult> GetAllLeaguesOverview()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/overview");
            AttachAuth(request);
            return await SendProxyAsync(_http, request);
        }

        #endregion

        #region Kullanıcı Lig Durumu

        /// <summary>
        /// Mevcut kullanıcının lig durumunu getirir
        /// </summary>
        [HttpGet("MyLeague")]
        public async Task<IActionResult> GetMyLeague()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/my-league");
            AttachAuth(request);
            return await SendProxyAsync(_http, request);
        }

        /// <summary>
        /// Belirli bir kullanıcının lig durumunu getirir
        /// </summary>
        [HttpGet("User/{userId}")]
        public async Task<IActionResult> GetUserLeague(int userId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/user/{userId}");
            request.Headers.Add("ngrok-skip-browser-warning", "true");
            return await SendProxyAsync(_http, request);
        }

        /// <summary>
        /// Kullanıcının lig geçmişini getirir
        /// </summary>
        [HttpGet("MyHistory")]
        public async Task<IActionResult> GetMyLeagueHistory()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/my-history");
            AttachAuth(request);
            return await SendProxyAsync(_http, request);
        }

        #endregion

        #region Lig Sıralaması

        /// <summary>
        /// Belirli bir ligteki kullanıcı sıralamasını getirir
        /// </summary>
        [HttpGet("{leagueId}/Leaderboard")]
        public async Task<IActionResult> GetLeagueLeaderboard(int leagueId, [FromQuery] int limit = 100)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/{leagueId}/leaderboard?limit={limit}");
            AttachAuth(request);
            return await SendProxyAsync(_http, request);
        }

        /// <summary>
        /// Mevcut kullanıcının ligindeki sıralamayı getirir
        /// </summary>
        [HttpGet("MyLeague/Leaderboard")]
        public async Task<IActionResult> GetMyLeagueLeaderboard([FromQuery] int limit = 100)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/my-league/leaderboard?limit={limit}");
            AttachAuth(request);
            return await SendProxyAsync(_http, request);
        }

        #endregion

        #region Lig Güncelleme

        /// <summary>
        /// Kullanıcının ligini kontrol eder ve gerekirse günceller
        /// </summary>
        [HttpPost("CheckUpdate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckAndUpdateLeague()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}/check-update");
            AttachAuth(request);
            return await SendProxyAsync(_http, request);
        }

        #endregion
    }
}



