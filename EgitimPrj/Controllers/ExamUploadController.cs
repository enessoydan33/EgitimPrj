using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EgitimPrj.Models.ExamUpload;
using EgitimPrj.Services.ExamUpload;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EgitimPrj.Controllers
{
    public class ExamUploadController : Controller
    {
        private readonly IPdfExamTextExtractor _pdfText;
        private readonly IPdfExamResultLineParser _lineParser;
        private readonly IExamStudentMatcher _matcher;
        private readonly ITeacherPanelStudentDirectoryService _studentDirectory;
        private readonly IExamTrialExamApiSaver _apiSaver;
        private readonly ILogger<ExamUploadController> _logger;

        public ExamUploadController(
            IPdfExamTextExtractor pdfText,
            IPdfExamResultLineParser lineParser,
            IExamStudentMatcher matcher,
            ITeacherPanelStudentDirectoryService studentDirectory,
            IExamTrialExamApiSaver apiSaver,
            ILogger<ExamUploadController> logger)
        {
            _pdfText = pdfText;
            _lineParser = lineParser;
            _matcher = matcher;
            _studentDirectory = studentDirectory;
            _apiSaver = apiSaver;
            _logger = logger;
        }

        /// <summary>/ExamUpload → manuel deneme girişi (TeacherExam).</summary>
        [HttpGet]
        public IActionResult Index()
        {
            if (!IsTeacher())
                return RedirectToAction("Login", "Teacher");

            return RedirectToAction("TeacherExam", "Teacher");
        }

        [HttpGet]
        public IActionResult Pdf()
        {
            if (!IsTeacher())
                return RedirectToAction("Login", "Teacher");

            ViewData["Title"] = "Deneme PDF Yükle";
            return View("Index");
        }

        /// <summary>PDF yükler, metin çıkarır ve TeacherPanel öğrencileriyle eşleştirir.</summary>
        [HttpPost]
        [RequestSizeLimit(25_000_000)]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UploadPdf(
            [FromForm] IFormFile? file,
            [FromForm] DateTime? examDate,
            CancellationToken cancellationToken)
        {
            if (!IsTeacher())
                return Unauthorized(new ExamUploadParseResponse { Success = false, ErrorMessage = "Öğretmen oturumu gerekli." });

            if (file is null || file.Length == 0)
                return BadRequest(new ExamUploadParseResponse { Success = false, ErrorMessage = "PDF dosyası seçilmedi." });

            var ext = Path.GetExtension(file.FileName);
            if (!string.Equals(ext, ".pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new ExamUploadParseResponse { Success = false, ErrorMessage = "Yalnızca .pdf kabul edilir." });

            var token = HttpContext.Session.GetString("TeacherToken");
            if (string.IsNullOrWhiteSpace(token))
                return Unauthorized(new ExamUploadParseResponse { Success = false, ErrorMessage = "Öğretmen token bulunamadı." });

            try
            {
                await using var ms = new MemoryStream();
                await file.CopyToAsync(ms, cancellationToken);
                ms.Position = 0;

                var lines = _pdfText.ExtractLines(ms);
                if (lines.Count == 0)
                    return BadRequest(new ExamUploadParseResponse
                    {
                        Success = false,
                        ErrorMessage = "PDF içinden metin çıkarılamadı (boş veya taranmış görüntü olabilir).",
                        RawTextPreview = null
                    });

                var date = examDate?.Date ?? DateTime.UtcNow.Date;
                var parsed = _lineParser.ParseLines(lines, date);
                if (parsed.Count == 0)
                {
                    return Ok(new ExamUploadParseResponse
                    {
                        Success = false,
                        ErrorMessage = "PDF metninden deneme satırı çıkarılamadı. Tablo biçimi farklı olabilir.",
                        RawTextPreview = PdfExamTextExtractor.Preview(lines, 2500)
                    });
                }

                var students = await _studentDirectory.GetAllStudentsAsync(token, cancellationToken);
                if (students.Count == 0)
                    _logger.LogWarning("TeacherPanel öğrenci listesi boş döndü; eşleştirme yapılamayacak.");

                var (matched, unmatched) = _matcher.Match(parsed, students);

                foreach (var u in unmatched)
                    _logger.LogInformation("PDF satırı eşleşmedi: {Name} No:{No}", u.StudentName, u.StudentNumber);

                return Ok(new ExamUploadParseResponse
                {
                    Success = true,
                    Matched = matched.ToList(),
                    Unmatched = unmatched.ToList(),
                    RawTextPreview = PdfExamTextExtractor.Preview(lines, 1200)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PDF işlenirken hata");
                return StatusCode(500, new ExamUploadParseResponse
                {
                    Success = false,
                    ErrorMessage = "PDF okunamadı veya ayrıştırılamadı: " + ex.Message
                });
            }
        }

        /// <summary>Eşleşen satırları CoMentor API (TeacherPanel trial-exams) üzerinden kaydeder.</summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Save([FromBody] ExamUploadSaveRequest? body, CancellationToken cancellationToken)
        {
            if (!IsTeacher())
                return Unauthorized(new ExamUploadSaveResponse { Success = false, Errors = { "Öğretmen oturumu gerekli." } });

            var token = HttpContext.Session.GetString("TeacherToken");
            if (string.IsNullOrWhiteSpace(token))
                return Unauthorized(new ExamUploadSaveResponse { Success = false, Errors = { "Öğretmen token bulunamadı." } });

            if (body is null || body.Rows is null || body.Rows.Count == 0)
                return BadRequest(new ExamUploadSaveResponse { Success = false, Errors = { "Geçerli kayıt gövdesi yok." } });

            try
            {
                var result = await _apiSaver.SaveAsync(body, token, cancellationToken);
                if (!result.Success && result.SavedCount == 0)
                    return StatusCode(502, result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API kaydı sırasında hata");
                return StatusCode(500, new ExamUploadSaveResponse
                {
                    Success = false,
                    Errors = { "Kayıt sırasında hata: " + ex.Message }
                });
            }
        }

        private bool IsTeacher() =>
            string.Equals(HttpContext.Session.GetString("IsTeacherLoggedIn"), "true", StringComparison.OrdinalIgnoreCase);
    }
}
