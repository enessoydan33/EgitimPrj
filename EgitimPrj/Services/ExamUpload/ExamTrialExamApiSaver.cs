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
using EgitimPrj.Models.Request;
using EgitimPrj.Models.Response;
using Microsoft.Extensions.Configuration;

namespace EgitimPrj.Services.ExamUpload
{
    public interface IExamTrialExamApiSaver
    {
        Task<ExamUploadSaveResponse> SaveAsync(
            ExamUploadSaveRequest request,
            string teacherBearerToken,
            CancellationToken cancellationToken = default);
    }

    public sealed class ExamTrialExamApiSaver : IExamTrialExamApiSaver
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ExamTrialExamApiSaver(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        private string TeacherPanelBase => $"{_configuration["ApiSettings:BaseUrl"]}/api/TeacherPanel";
        private string SubjectApiUrl => $"{_configuration["ApiSettings:BaseUrl"]}/api/Subject";

        public async Task<ExamUploadSaveResponse> SaveAsync(
            ExamUploadSaveRequest request,
            string teacherBearerToken,
            CancellationToken cancellationToken = default)
        {
            var http = _httpClientFactory.CreateClient();
            var resp = new ExamUploadSaveResponse { Success = true };
            if (request.Rows is null || request.Rows.Count == 0)
            {
                resp.Success = false;
                resp.Errors.Add("Kaydedilecek satır yok.");
                return resp;
            }

            var examType = string.IsNullOrWhiteSpace(request.ExamType) ? "TYT" : request.ExamType.Trim();
            List<SubjectDto>? subjects;
            using (var subReq = new HttpRequestMessage(
                       HttpMethod.Get,
                       $"{SubjectApiUrl}?examType={Uri.EscapeDataString(examType)}"))
            {
                AttachTeacher(teacherBearerToken, subReq);
                using var subRes = await http.SendAsync(subReq, cancellationToken);
                if (!subRes.IsSuccessStatusCode)
                {
                    resp.Success = false;
                    resp.Errors.Add($"Ders listesi alınamadı (HTTP {(int)subRes.StatusCode}).");
                    return resp;
                }

                subjects = await subRes.Content.ReadFromJsonAsync<List<SubjectDto>>(cancellationToken: cancellationToken);
            }

            if (subjects is null || subjects.Count == 0)
            {
                resp.Success = false;
                resp.Errors.Add("TYT ders listesi boş.");
                return resp;
            }

            foreach (var row in request.Rows)
            {
                if (row.StudentId <= 0 || row.ClassroomId <= 0)
                {
                    resp.FailedCount++;
                    resp.Errors.Add($"Geçersiz öğrenci/sınıf: {row.StudentId}");
                    continue;
                }

                CreateTrialExamRequest body;
                try
                {
                    body = TyNetsToTrialExamMapper.BuildRequest(
                        request.ExamName,
                        request.ExamDate,
                        row.TurkishNet,
                        row.MathNet,
                        row.ScienceNet,
                        row.SocialNet,
                        subjects);
                }
                catch (Exception ex)
                {
                    resp.FailedCount++;
                    resp.Errors.Add($"Satır {row.StudentId}: {ex.Message}");
                    continue;
                }

                if (body.SubjectScores.Count == 0)
                {
                    resp.FailedCount++;
                    resp.Errors.Add($"Satır {row.StudentId}: ders skoru üretilemedi.");
                    continue;
                }

                var url =
                    $"{TeacherPanelBase}/classrooms/{row.ClassroomId}/students/{row.StudentId}/trial-exams";
                var jsonOpts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                using var post = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = JsonContent.Create(body, mediaType: null, jsonOpts)
                };
                AttachTeacher(teacherBearerToken, post);
                using var postRes = await http.SendAsync(post, cancellationToken);
                if (postRes.IsSuccessStatusCode)
                    resp.SavedCount++;
                else
                {
                    resp.FailedCount++;
                    var err = await postRes.Content.ReadAsStringAsync(cancellationToken);
                    resp.Errors.Add($"Öğrenci {row.StudentId}: HTTP {(int)postRes.StatusCode} {err}");
                }
            }

            resp.Success = resp.FailedCount == 0;
            return resp;
        }

        private static void AttachTeacher(string token, HttpRequestMessage req)
        {
            if (!string.IsNullOrWhiteSpace(token))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Headers.TryAddWithoutValidation("ngrok-skip-browser-warning", "true");
        }
    }
}
