using EgitimPrj.Models.Request;
using EgitimPrj.Models.ViewModel.ExamViewModel;
using EgitimPrj.Models.Response;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace EgitimPrj.Controllers
{
    public class ExamController : Controller
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _configuration;
        // CoMentor.API TrialExam endpoint'i
        private string TrialExamApiUrl => $"{_configuration["ApiSettings:BaseUrl"]}/api/TrialExam";
        // CoMentor.API Subject endpoint'i
        private string SubjectApiUrl => $"{_configuration["ApiSettings:BaseUrl"]}/api/Subject";

        public ExamController(HttpClient http, IConfiguration configuration)
        {
            _http = http;
            _configuration = configuration;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateExamViewModel model)
        {
            if (model.SubjectScores is null || model.SubjectScores.Count == 0)
            {
                ModelState.AddModelError(nameof(model.SubjectScores), "En az bir ders ekleyin.");
            }

            if (!ModelState.IsValid)
            {
                EnsureSubjectDefaults(model);
                return View("~/Views/Dashboard/ExamTracking.cshtml", model);
            }

            var apiRequest = new CreateTrialExamRequest
            {
                Name = model.Name,
                ExamType = model.ExamType,
                ExamDate = model.ExamDate,
                DurationMinutes = model.DurationMinutes,
                Notes = model.Notes,
                SubjectScores = model.SubjectScores.Select(s => new SubjectScoreRequest
                {
                    SubjectId = s.SubjectId,
                    CorrectAnswers = s.CorrectAnswers,
                    WrongAnswers = s.WrongAnswers,
                    EmptyAnswers = s.EmptyAnswers
                }).ToList()
            };

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, TrialExamApiUrl)
            {
                Content = JsonContent.Create(apiRequest)
            };

            var token = HttpContext.Session.GetString("Token");
            if (!string.IsNullOrWhiteSpace(token))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            try
            {
                var response = await _http.SendAsync(requestMessage);

                if (response.IsSuccessStatusCode)
                {
                    TempData["ExamSuccess"] = "Deneme sonucu kaydedildi.";
                    return RedirectToAction("ExamTracking", "Dashboard");
                }

                var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, string[]>>();
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
                    ModelState.AddModelError(string.Empty, $"Deneme kaydedilemedi (HTTP {(int)response.StatusCode}).");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "API isteği sırasında hata oluştu: " + ex.Message);
            }

            EnsureSubjectDefaults(model);
            return View("~/Views/Dashboard/ExamTracking.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, TrialExamApiUrl);
            AttachAuth(request);

            try
            {
                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<TrialExamListResponse>();
                    return Json(data);
                }

                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, $"API hatası: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{TrialExamApiUrl}/{id}");
            AttachAuth(request);

            try
            {
                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<TrialExamDto>();
                    return Json(data);
                }

                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, $"API hatası: {ex.Message}");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, UpdateExamViewModel model)
        {
            var apiRequest = new UpdateTrialExamRequest
            {
                Name = model.Name,
                ExamDate = model.ExamDate,
                DurationMinutes = model.DurationMinutes,
                Notes = model.Notes,
                SubjectScores = model.SubjectScores?.Select(s => new SubjectScoreRequest
                {
                    SubjectId = s.SubjectId,
                    CorrectAnswers = s.CorrectAnswers,
                    WrongAnswers = s.WrongAnswers,
                    EmptyAnswers = s.EmptyAnswers
                }).ToList()
            };

            var request = new HttpRequestMessage(HttpMethod.Put, $"{TrialExamApiUrl}/{id}")
            {
                Content = JsonContent.Create(apiRequest)
            };
            AttachAuth(request);

            try
            {
                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    TempData["ExamSuccess"] = "Deneme güncellendi.";
                    return RedirectToAction("ExamTracking", "Dashboard");
                }

                var body = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Güncelleme başarısız (HTTP {(int)response.StatusCode}): {body}");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "API isteği sırasında hata oluştu: " + ex.Message);
            }

            model.SubjectScores ??= new List<SubjectScoreViewModel> { new SubjectScoreViewModel() };
            return View("~/Views/Dashboard/ExamTracking.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, $"{TrialExamApiUrl}/{id}");
            AttachAuth(request);

            try
            {
                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    TempData["ExamSuccess"] = "Deneme silindi.";
                    return RedirectToAction("ExamTracking", "Dashboard");
                }

                TempData["ExamError"] = $"Deneme silinemedi (HTTP {(int)response.StatusCode}).";
            }
            catch (Exception ex)
            {
                TempData["ExamError"] = "API isteği sırasında hata oluştu: " + ex.Message;
            }

            return RedirectToAction("ExamTracking", "Dashboard");
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

        [HttpGet]
        public async Task<IActionResult> Subjects(string? examType = null)
        {
            var url = string.IsNullOrWhiteSpace(examType)
                ? SubjectApiUrl
                : $"{SubjectApiUrl}?examType={Uri.EscapeDataString(examType)}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            // Ngrok tarayıcı uyarısını atlamak için gerekli header
            request.Headers.Add("ngrok-skip-browser-warning", "true");

            try
            {
                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<List<SubjectDto>>();
                    return Json(data);
                }

                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, $"API hatası: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Stats(string? examType = null)
        {
            var url = string.IsNullOrWhiteSpace(examType)
                ? $"{TrialExamApiUrl}/stats"
                : $"{TrialExamApiUrl}/stats?examType={Uri.EscapeDataString(examType)}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            AttachAuth(request);

            try
            {
                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<TrialExamStatsDto>();
                    return Json(data);
                }

                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, $"API hatası: {ex.Message}");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSubject(CreateSubjectRequest model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, errors = ModelState });

            var request = new HttpRequestMessage(HttpMethod.Post, SubjectApiUrl)
            {
                Content = JsonContent.Create(model)
            };
            AttachAuth(request);

            try
            {
                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<SubjectDto>();
                    return Json(new { success = true, data });
                }

                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { success = false, error });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeedSubjects()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{SubjectApiUrl}/seed");
            AttachAuth(request);

            try
            {
                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<object>();
                    return Json(new { success = true, data });
                }

                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { success = false, error });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new { success = false, error = ex.Message });
            }
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSubject(int id)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, $"{SubjectApiUrl}/{id}");
            AttachAuth(request);

            try
            {
                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NoContent)
                {
                    return Json(new { success = true });
                }

                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { success = false, error });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new { success = false, error = ex.Message });
            }
        }

        private static void EnsureSubjectDefaults(CreateExamViewModel model)
        {
            model.SubjectScores ??= new List<SubjectScoreViewModel>();
            if (model.SubjectScores.Count == 0)
            {
                model.SubjectScores.Add(new SubjectScoreViewModel());
            }

            if (model.ExamDate == default)
            {
                model.ExamDate = DateTime.Today;
            }
        }
    }
}
