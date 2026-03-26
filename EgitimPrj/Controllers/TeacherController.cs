using System.Net.Http.Json;
using EgitimPrj.Models.Response;
using EgitimPrj.Models.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace EgitimPrj.Controllers
{
    public class TeacherController : Controller
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _configuration;

        // CoMentor TeacherAuth endpoint'i
        private string TeacherAuthBase => $"{_configuration["ApiSettings:BaseUrl"]}/api/TeacherAuth";

        public TeacherController(HttpClient http, IConfiguration configuration)
        {
            _http = http;
            _configuration = configuration;
        }

        #region Views

        public IActionResult TeacherPanel()
        {
            var isTeacherLoggedIn = HttpContext.Session.GetString("IsTeacherLoggedIn");
            if (!string.Equals(isTeacherLoggedIn, "true", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(Login));
            }

            ViewData["Title"] = "Öğretmen Paneli";
            return View();
        }

        public IActionResult TeacherHomework()
        {
            var isTeacherLoggedIn = HttpContext.Session.GetString("IsTeacherLoggedIn");
            if (!string.Equals(isTeacherLoggedIn, "true", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(Login));
            }

            ViewData["Title"] = "Ödev Gönder";
            return View();
        }

        public IActionResult TeacherAnnouncement()
        {
            var isTeacherLoggedIn = HttpContext.Session.GetString("IsTeacherLoggedIn");
            if (!string.Equals(isTeacherLoggedIn, "true", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(Login));
            }

            ViewData["Title"] = "Duyuru Gönder";
            return View();
        }

        public IActionResult TeacherExam()
        {
            var isTeacherLoggedIn = HttpContext.Session.GetString("IsTeacherLoggedIn");
            if (!string.Equals(isTeacherLoggedIn, "true", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(Login));
            }

            ViewData["Title"] = "Öğrenci Denemesi Ekle";
            return View();
        }

        public IActionResult StudentProfile(int id)
        {
            var isTeacherLoggedIn = HttpContext.Session.GetString("IsTeacherLoggedIn");
            if (!string.Equals(isTeacherLoggedIn, "true", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(Login));
            }

            ViewData["Title"] = "Öğrenci Profili";
            ViewData["StudentId"] = id;
            return View();
        }

        #endregion

        #region Teacher Auth

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var request = new HttpRequestMessage(HttpMethod.Post, $"{TeacherAuthBase}/login")
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
                    var data = await res.Content.ReadFromJsonAsync<LoginResponseModel>();

                    HttpContext.Session.SetString("IsTeacherLoggedIn", "true");
                    HttpContext.Session.SetString("TeacherName", data?.UserName ?? model.Email);
                    if (data is not null && data.UserId > 0)
                        HttpContext.Session.SetString("UserId", data.UserId.ToString());
                    if (!string.IsNullOrWhiteSpace(data?.Token))
                        HttpContext.Session.SetString("TeacherToken", data!.Token);

                    // Öğretmen paneline yönlendir
                    return RedirectToAction(nameof(TeacherPanel));
                }

                model.Password = string.Empty;
                ModelState.Remove(nameof(model.Password));
                ModelState.AddModelError(string.Empty, "Öğretmen girişi başarısız. Bilgilerinizi kontrol edin.");
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Sunucuya ulaşılamadı: " + ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var body = new
            {
                Name = model.Name,
                Surname = model.SurName,
                model.Email,
                model.Password
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{TeacherAuthBase}/register")
            {
                Content = JsonContent.Create(body)
            };
            request.Headers.Add("ngrok-skip-browser-warning", "true");

            try
            {
                var res = await _http.SendAsync(request);
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<LoginResponseModel>();

                    HttpContext.Session.SetString("IsTeacherLoggedIn", "true");
                    HttpContext.Session.SetString("TeacherName", data?.UserName ?? model.Email);
                    if (data is not null && data.UserId > 0)
                        HttpContext.Session.SetString("UserId", data.UserId.ToString());
                    if (!string.IsNullOrWhiteSpace(data?.Token))
                        HttpContext.Session.SetString("TeacherToken", data!.Token);

                    return RedirectToAction(nameof(TeacherPanel));
                }

                var problem = await res.Content.ReadFromJsonAsync<Dictionary<string, string[]>>();
                if (problem is not null)
                {
                    foreach (var kv in problem)
                    {
                        foreach (var msg in kv.Value)
                        {
                            ModelState.AddModelError(kv.Key ?? string.Empty, msg);
                        }
                    }
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

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("IsTeacherLoggedIn");
            HttpContext.Session.Remove("TeacherName");
            HttpContext.Session.Remove("TeacherToken");
            return RedirectToAction(nameof(Login));
        }

        #endregion
    }
}
