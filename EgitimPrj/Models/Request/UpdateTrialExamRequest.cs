using System;
using System.Collections.Generic;

namespace EgitimPrj.Models.Request
{
    public class UpdateTrialExamRequest
    {
        public string? Name { get; set; }
        public DateTime? ExamDate { get; set; }
        public int? DurationMinutes { get; set; }
        public string? Notes { get; set; }
        public List<SubjectScoreRequest>? SubjectScores { get; set; }
    }
}
























