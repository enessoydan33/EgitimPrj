using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;

namespace EgitimPrj.Controllers
{
    /// <summary>
    /// CoMentor TeacherPanel API'si için proxy controller.
    /// View'lar JavaScript ile bu controller'a istek atar, bu controller da
    /// istekleri arka plandaki CoMentor.API'ye iletir.
    /// </summary>
    public class TeacherPanelProxyController : Controller
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _configuration;

        private string TeacherPanelBase => $"{_configuration["ApiSettings:BaseUrl"]}/api/TeacherPanel";

        public TeacherPanelProxyController(HttpClient http, IConfiguration configuration)
        {
            _http = http;
            _configuration = configuration;
        }

        #region Classrooms

        [HttpGet]
        public async Task<IActionResult> Classrooms()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{TeacherPanelBase}/classrooms");
            AttachTeacherAuth(request);

            return await Send(request);
        }

        [HttpPost]
        public async Task<IActionResult> CreateClassroom([FromBody] System.Text.Json.JsonElement body)
        {
            var jsonString = System.Text.Json.JsonSerializer.Serialize(body);
            var request = new HttpRequestMessage(HttpMethod.Post, $"{TeacherPanelBase}/classrooms")
            {
                Content = new StringContent(jsonString, System.Text.Encoding.UTF8, "application/json")
            };
            AttachTeacherAuth(request);
            return await Send(request);
        }

        [HttpPost]
        public async Task<IActionResult> AddStudentToClassroom(int classroomId, int studentId)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{TeacherPanelBase}/classrooms/{classroomId}/students/{studentId}");

            AttachTeacherAuth(request);
            return await Send(request);
        }

        [HttpDelete]
        public async Task<IActionResult> RemoveStudentFromClassroom(int classroomId, int studentId)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Delete,
                $"{TeacherPanelBase}/classrooms/{classroomId}/students/{studentId}");

            AttachTeacherAuth(request);
            return await Send(request);
        }

        #endregion

        #region Announcements

        [HttpGet]
        public async Task<IActionResult> Announcements()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{TeacherPanelBase}/announcements");
            AttachTeacherAuth(request);
            return await Send(request);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAnnouncement([FromBody] System.Text.Json.JsonElement body)
        {
            var jsonString = System.Text.Json.JsonSerializer.Serialize(body);
            var request = new HttpRequestMessage(HttpMethod.Post, $"{TeacherPanelBase}/announcements")
            {
                Content = new StringContent(jsonString, System.Text.Encoding.UTF8, "application/json")
            };
            AttachTeacherAuth(request);
            return await Send(request);
        }

        #endregion

        #region Homeworks

        [HttpGet]
        public async Task<IActionResult> Homeworks()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{TeacherPanelBase}/homeworks");
            AttachTeacherAuth(request);
            return await Send(request);
        }

        [HttpPost]
        public async Task<IActionResult> CreateHomework([FromBody] System.Text.Json.JsonElement body)
        {
            // Convert to a dictionary so we can manipulate the Date
            var dict = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(body.ToString());
            if (dict != null && dict.TryGetValue("dueDate", out var dueDateObj))
            {
                if (DateTime.TryParse(dueDateObj?.ToString(), out DateTime parsedDate))
                {
                    dict["dueDate"] = parsedDate.ToUniversalTime().ToString("O"); 
                }
            }

            var jsonString = System.Text.Json.JsonSerializer.Serialize(dict);
            var request = new HttpRequestMessage(HttpMethod.Post, $"{TeacherPanelBase}/homeworks")
            {
                Content = new StringContent(jsonString, System.Text.Encoding.UTF8, "application/json")
            };
            AttachTeacherAuth(request);
            return await Send(request);
        }

        #endregion

        #region Student Monitoring / Trial Exams

        [HttpGet]
        public async Task<IActionResult> ClassroomPerformances(int classroomId)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{TeacherPanelBase}/classrooms/{classroomId}/performances");

            AttachTeacherAuth(request);
            return await Send(request);
        }

        [HttpPost]
        public async Task<IActionResult> AddStudentTrialExam(
            int classroomId,
            int studentId,
            [FromBody] System.Text.Json.JsonElement body)
        {
            var jsonString = System.Text.Json.JsonSerializer.Serialize(body);
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{TeacherPanelBase}/classrooms/{classroomId}/students/{studentId}/trial-exams")
            {
                Content = new StringContent(jsonString, System.Text.Encoding.UTF8, "application/json")
            };
            AttachTeacherAuth(request);
            return await Send(request);
        }

        [HttpGet]
        public async Task<IActionResult> StudentTrialExams(
            int classroomId,
            int studentId,
            string? examType = null)
        {
            var url = $"{TeacherPanelBase}/classrooms/{classroomId}/students/{studentId}/trial-exams";
            if (!string.IsNullOrWhiteSpace(examType))
            {
                url += $"?examType={Uri.EscapeDataString(examType)}";
            }

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            AttachTeacherAuth(request);
            return await Send(request);
        }

        [HttpGet]
        public async Task<IActionResult> StudentTrialExamDetail(
            int classroomId,
            int studentId,
            int trialId)
        {
            var url = $"{TeacherPanelBase}/classrooms/{classroomId}/students/{studentId}/trial-exams/{trialId}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            AttachTeacherAuth(request);
            return await Send(request);
        }

        #endregion

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

        private void AttachTeacherAuth(HttpRequestMessage request)
        {
            // Öğretmen JWT token'ı için ayrı bir session anahtarı kullanıyoruz
            var token = HttpContext.Session.GetString("TeacherToken");
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Ngrok uyarısını bastıran header, diğer proxy controller'larla aynı
            request.Headers.Add("ngrok-skip-browser-warning", "true");
        }
    }
}

