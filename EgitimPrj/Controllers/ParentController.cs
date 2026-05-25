using EgitimPrj.Models.Response;
using EgitimPrj.Models.ViewModel;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json;

namespace EgitimPrj.Controllers
{
    public class ParentController : Controller
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _configuration;

        private string ParentAuthBase
        {
            get
            {
                var baseUrl = _configuration["ApiSettings:BaseUrl"]?.TrimEnd('/') ?? string.Empty;
                var path = _configuration["ApiPaths:ParentAuth"] ?? "/api/ParentAuth";
                return $"{baseUrl}{path}";
            }
        }

        public ParentController(HttpClient http, IConfiguration configuration)
        {
            _http = http;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Login()
        {
            var isParentLoggedIn = HttpContext.Session.GetString("IsParentLoggedIn");
            if (string.Equals(isParentLoggedIn, "true", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("ParentSummary", "Dashboard");
            }

            ViewData["Title"] = "Veli Girişi";
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var request = new HttpRequestMessage(HttpMethod.Post, $"{ParentAuthBase}/login")
            {
                Content = JsonContent.Create(new
                {
                    model.Email,
                    model.Password
                })
            };
            request.Headers.Add("ngrok-skip-browser-warning", "true");

            try
            {
                var res = await _http.SendAsync(request);
                if (res.IsSuccessStatusCode)
                {
                    var raw = await res.Content.ReadAsStringAsync();
                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    LoginResponseModel? data = null;
                    string? token = null;

                    if (!string.IsNullOrWhiteSpace(raw))
                    {
                        using var doc = JsonDocument.Parse(raw);
                        data = doc.RootElement.Deserialize<LoginResponseModel>(jsonOptions);
                        token = BearerTokenNormalizer.Normalize(BearerTokenNormalizer.FindInLoginJson(doc.RootElement))
                            ?? BearerTokenNormalizer.Normalize(data?.Token);
                    }

                    if (string.IsNullOrWhiteSpace(token))
                    {
                        model.Password = string.Empty;
                        ModelState.Remove(nameof(model.Password));
                        ModelState.AddModelError(string.Empty, "Veli girişi başarısız. Token bilgisi alınamadı.");
                        return View(model);
                    }

                    HttpContext.Session.SetString("IsParentLoggedIn", "true");
                    HttpContext.Session.SetString("ParentName", data?.UserName ?? model.Email);
                    HttpContext.Session.SetString("Name", data?.UserName ?? model.Email);

                    if (data is not null && data.UserId > 0)
                        HttpContext.Session.SetString("ParentId", data.UserId.ToString());

                    HttpContext.Session.SetString("ParentToken", token);

                    return RedirectToAction("ParentSummary", "Dashboard");
                }

                model.Password = string.Empty;
                ModelState.Remove(nameof(model.Password));
                ModelState.AddModelError(string.Empty, "Veli girişi başarısız. Bilgilerinizi kontrol edin.");
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Sunucuya ulaşılamadı: " + ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("IsParentLoggedIn");
            HttpContext.Session.Remove("ParentName");
            HttpContext.Session.Remove("ParentToken");
            HttpContext.Session.Remove("ParentId");
            HttpContext.Session.Remove("Name");

            return RedirectToAction(nameof(Login));
        }
    }
}

