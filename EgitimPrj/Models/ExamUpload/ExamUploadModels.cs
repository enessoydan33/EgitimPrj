using System;
using System.Collections.Generic;
using EgitimPrj.Models.Request;

namespace EgitimPrj.Models.ExamUpload
{
    public class ExamUploadParseResponse
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RawTextPreview { get; set; }
        public List<ExamResult> Matched { get; set; } = new();
        public List<ExamResult> Unmatched { get; set; } = new();
    }

    public class ExamUploadSaveRequest
    {
        public string ExamName { get; set; } = "PDF Toplu Deneme";
        public string ExamType { get; set; } = "TYT";
        public DateTime ExamDate { get; set; } = DateTime.UtcNow.Date;
        public List<ExamUploadSaveRow> Rows { get; set; } = new();
    }

    public class ExamUploadSaveRow
    {
        public int StudentId { get; set; }
        public int ClassroomId { get; set; }
        public double TurkishNet { get; set; }
        public double MathNet { get; set; }
        public double ScienceNet { get; set; }
        public double SocialNet { get; set; }
        public double TotalNet { get; set; }
    }

    public class ExamUploadSaveResponse
    {
        public bool Success { get; set; }
        public int SavedCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// İleride toplu kayıt endpoint'i eklendiğinde kullanılmak üzere hazır DTO.
    /// </summary>
    public class BulkTrialExamApiPayload
    {
        public string ExamName { get; set; } = null!;
        public string ExamType { get; set; } = null!;
        public DateTime ExamDate { get; set; }
        public List<BulkTrialExamStudentPayload> Students { get; set; } = new();
    }

    public class BulkTrialExamStudentPayload
    {
        public int StudentId { get; set; }
        public int ClassroomId { get; set; }
        public CreateTrialExamRequest Exam { get; set; } = null!;
    }
}
