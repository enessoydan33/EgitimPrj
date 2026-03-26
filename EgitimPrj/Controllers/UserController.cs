using EgitimPrj.Models.Response;
using EgitimPrj.Models.ViewModel;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace EgitimPrj.Controllers
{
    public class UserController: Controller
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _configuration;
        private string ApiBase => $"{_configuration["ApiSettings:BaseUrl"]}/api/auth";

        public UserController(HttpClient http, IConfiguration configuration)
        {
            _http = http;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new LoginViewModel());
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var apiBody = new
            {
                model.Name,
                model.SurName,
                model.Email,
                model.Password,
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}/register")
            {
                Content = JsonContent.Create(apiBody)
            };
            // Ngrok tarayıcı uyarısını atlamak için gerekli header
            request.Headers.Add("ngrok-skip-browser-warning", "true");

            try
            {
                var res = await _http.SendAsync(request);

                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<LoginResponseModel>();

                    HttpContext.Session.SetString("IsLoggedIn", "true");
                    HttpContext.Session.SetString("UserName", data?.UserName ?? model.Email);
                    if (data is not null && data.UserId > 0)
                        HttpContext.Session.SetString("UserId", data.UserId.ToString());
                    if (!string.IsNullOrWhiteSpace(data?.Token))
                        HttpContext.Session.SetString("Token", data!.Token);

                    return RedirectToAction("Index", "Dashboard");
                }

                var problem = await res.Content.ReadFromJsonAsync<Dictionary<string, string[]>>();
                if (problem is not null && problem.TryGetValue("errors", out var _))
                {
                    foreach (var kv in problem)
                        foreach (var msg in kv.Value)
                            ModelState.AddModelError(kv.Key ?? string.Empty, msg);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, $"Kayıt başarısız (HTTP {(int)res.StatusCode}).");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Sunucuya ulaşılamadı: " + ex.Message);
            }

            return View(model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Index(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var apiBody = new
            {
                model.Email,
                model.Password
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}/login")
            {
                Content = JsonContent.Create(apiBody)
            };
            // Ngrok tarayıcı uyarısını atlamak için gerekli header
            request.Headers.Add("ngrok-skip-browser-warning", "true");

            try
            {
                var res = await _http.SendAsync(request);

                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<LoginResponseModel>();

                    HttpContext.Session.SetString("IsLoggedIn", "true");
                    HttpContext.Session.SetString("UserName", data?.UserName ?? model.Email);
                    if (data is not null && data.UserId > 0)
                        HttpContext.Session.SetString("UserId", data.UserId.ToString());
                    if (!string.IsNullOrWhiteSpace(data?.Token))
                        HttpContext.Session.SetString("Token", data!.Token);

                    return RedirectToAction("Index", "Dashboard");
                }

                model.Password = string.Empty;
                ModelState.Remove(nameof(model.Password));

                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Sunucuya ulaşılamadı: " + ex.Message);
                return View(model);
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "User");
        }
    }
}
