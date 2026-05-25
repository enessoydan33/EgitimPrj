using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EgitimPrj.Models.ExamUpload;
using Microsoft.Extensions.Configuration;
namespace EgitimPrj.Services.ExamUpload
{
    public interface ITeacherPanelStudentDirectoryService
    {
        Task<IReadOnlyList<TeacherPanelStudentInfo>> GetAllStudentsAsync(
            string teacherBearerToken,
            CancellationToken cancellationToken = default);
    }

    public sealed class TeacherPanelStudentDirectoryService : ITeacherPanelStudentDirectoryService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public TeacherPanelStudentDirectoryService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        private string TeacherPanelBase => $"{_configuration["ApiSettings:BaseUrl"]}/api/TeacherPanel";

        public async Task<IReadOnlyList<TeacherPanelStudentInfo>> GetAllStudentsAsync(
            string teacherBearerToken,
            CancellationToken cancellationToken = default)
        {
            var http = _httpClientFactory.CreateClient();
            var list = new List<TeacherPanelStudentInfo>();
            using var classroomsReq = new HttpRequestMessage(HttpMethod.Get, $"{TeacherPanelBase}/classrooms");
            Attach(teacherBearerToken, classroomsReq);
            using var classroomsRes = await http.SendAsync(classroomsReq, cancellationToken);
            if (!classroomsRes.IsSuccessStatusCode)
                return list;

            var classrooms = await classroomsRes.Content.ReadFromJsonAsync<List<JsonElement>>(cancellationToken);
            if (classrooms is null)
                return list;

            foreach (var c in classrooms)
            {
                if (!c.TryGetProperty("id", out var idEl) || idEl.ValueKind != JsonValueKind.Number)
                    continue;
                var classroomId = idEl.GetInt32();

                using var perfReq = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"{TeacherPanelBase}/classrooms/{classroomId}/performances");
                Attach(teacherBearerToken, perfReq);
                using var perfRes = await http.SendAsync(perfReq, cancellationToken);
                if (!perfRes.IsSuccessStatusCode)
                    continue;

                var performances = await perfRes.Content.ReadFromJsonAsync<List<JsonElement>>(cancellationToken);
                if (performances is null)
                    continue;

                foreach (var p in performances)
                {
                    if (!p.TryGetProperty("studentId", out var sid) || sid.ValueKind != JsonValueKind.Number)
                        continue;
                    var studentId = sid.GetInt32();
                    var name = ReadString(p, "studentName", "StudentName", "name", "Name", "fullName", "FullName");
                    var number = ReadString(p, "studentNumber", "StudentNumber", "schoolNumber", "SchoolNumber", "studentNo", "StudentNo");

                    list.Add(new TeacherPanelStudentInfo
                    {
                        StudentId = studentId,
                        FullName = name ?? string.Empty,
                        StudentNumber = number,
                        ClassroomId = classroomId
                    });
                }
            }

            return list;
        }

        private static string? ReadString(JsonElement obj, params string[] names)
        {
            foreach (var n in names)
            {
                if (obj.TryGetProperty(n, out var el) && el.ValueKind == JsonValueKind.String)
                {
                    var s = el.GetString();
                    if (!string.IsNullOrWhiteSpace(s))
                        return s;
                }
            }

            return null;
        }

        private static void Attach(string token, HttpRequestMessage req)
        {
            if (!string.IsNullOrWhiteSpace(token))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Headers.TryAddWithoutValidation("ngrok-skip-browser-warning", "true");
        }
    }
}
