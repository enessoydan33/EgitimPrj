using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EgitimPrj.Models.ViewModel.ExamViewModel
{
    public class UpdateExamViewModel
    {
        [StringLength(100, MinimumLength = 1)]
        public string? Name { get; set; }

        public DateTime? ExamDate { get; set; }

        public int? DurationMinutes { get; set; }

        public string? Notes { get; set; }

        public List<SubjectScoreViewModel>? SubjectScores { get; set; }
    }
}
























