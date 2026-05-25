using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace EgitimPrj.Controllers
{
    /// <summary>
    /// CoMentor TeacherPanel API'si için proxy controller.
    /// View'lar JavaScript ile bu controller'a istek atar, bu controller da
    /// istekleri arka plandaki CoMentor.API'ye iletir.
    /// </summary>
    public class TeacherPanelProxyController : ProxyControllerBase
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
            AttachAuth(request, "TeacherToken");
            return await SendProxyAsync(_http, request);
        }

        [HttpPost]
        public async Task<IActionResult> CreateClassroom([FromBody] System.Text.Json.JsonElement body)
        {
            var jsonString = System.Text.Json.JsonSerializer.Serialize(body);
            var request = new HttpRequestMessage(HttpMethod.Post, $"{TeacherPanelBase}/classrooms")
            {
                Content = new StringContent(jsonString, System.Text.Encoding.UTF8, "application/json")
            };
            AttachAuth(request, "TeacherToken");
            return await SendProxyAsync(_http, request);
        }

        [HttpPost]
        public async Task<IActionResult> AddStudentToClassroom(int classroomId, int studentId)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{TeacherPanelBase}/classrooms/{classroomId}/students/{studentId}");

            AttachAuth(request, "TeacherToken");
            return await SendProxyAsync(_http, request);
        }

        [HttpDelete]
        public async Task<IActionResult> RemoveStudentFromClassroom(int classroomId, int studentId)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Delete,
                $"{TeacherPanelBase}/classrooms/{classroomId}/students/{studentId}");

            AttachAuth(request, "TeacherToken");
            return await SendProxyAsync(_http, request);
        }

        #endregion

        #region Announcements

        [HttpGet]
        public async Task<IActionResult> Announcements()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{TeacherPanelBase}/announcements");
            AttachAuth(request, "TeacherToken");
            return await SendProxyAsync(_http, request);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAnnouncement([FromBody] System.Text.Json.JsonElement body)
        {
            var jsonString = System.Text.Json.JsonSerializer.Serialize(body);
            var request = new HttpRequestMessage(HttpMethod.Post, $"{TeacherPanelBase}/announcements")
            {
                Content = new StringContent(jsonString, System.Text.Encoding.UTF8, "application/json")
            };
            AttachAuth(request, "TeacherToken");
            return await SendProxyAsync(_http, request);
        }

        #endregion

        #region Homeworks

        [HttpGet]
        public async Task<IActionResult> Homeworks()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{TeacherPanelBase}/homeworks");
            AttachAuth(request, "TeacherToken");
            return await SendProxyAsync(_http, request);
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
            AttachAuth(request, "TeacherToken");
            return await SendProxyAsync(_http, request);
        }

        #endregion

        #region Student Monitoring / Trial Exams

        [HttpGet]
        public async Task<IActionResult> ClassroomPerformances(int classroomId)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{TeacherPanelBase}/classrooms/{classroomId}/performances");

            AttachAuth(request, "TeacherToken");
            return await SendProxyAsync(_http, request);
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
            AttachAuth(request, "TeacherToken");
            return await SendProxyAsync(_http, request);
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
            AttachAuth(request, "TeacherToken");
            return await SendProxyAsync(_http, request);
        }

        [HttpGet]
        public async Task<IActionResult> StudentTrialExamDetail(
            int classroomId,
            int studentId,
            int trialId)
        {
            var url = $"{TeacherPanelBase}/classrooms/{classroomId}/students/{studentId}/trial-exams/{trialId}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            AttachAuth(request, "TeacherToken");
            return await SendProxyAsync(_http, request);
        }

        #endregion

        #region Appointments

        [HttpGet]
        public async Task<IActionResult> Appointments()
        {
            var denied = RequireSessionBearer("TeacherToken", "Öğretmen oturumunda token yok. Lütfen tekrar giriş yapın.");
            if (denied != null)
                return denied;

            var request = new HttpRequestMessage(HttpMethod.Get, $"{TeacherPanelBase}/appointments");
            AttachAuth(request, "TeacherToken");
            return await SendProxyAsync(_http, request);
        }

        [HttpPut]
        public async Task<IActionResult> ScheduleAppointment(int id, [FromBody] System.Text.Json.JsonElement body)
        {
            var denied = RequireSessionBearer("TeacherToken", "Öğretmen oturumunda token yok. Lütfen tekrar giriş yapın.");
            if (denied != null)
                return denied;

            var jsonString = System.Text.Json.JsonSerializer.Serialize(body);
            var request = new HttpRequestMessage(HttpMethod.Put, $"{TeacherPanelBase}/appointments/{id}/schedule")
            {
                Content = new StringContent(jsonString, Encoding.UTF8, "application/json")
            };
            AttachAuth(request, "TeacherToken");
            return await SendProxyAsync(_http, request);
        }

        #endregion

    }
}

