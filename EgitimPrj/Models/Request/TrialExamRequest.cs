using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EgitimPrj.Models.Request
{
    public class SubjectScoreRequest
    {
        [Required]
        public int SubjectId { get; set; }

        [Range(0, 100)]
        public int CorrectAnswers { get; set; }

        [Range(0, 100)]
        public int WrongAnswers { get; set; }

        [Range(0, 100)]
        public int EmptyAnswers { get; set; }
    }

    public class CreateTrialExamRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = null!;

        [Required]
        [RegularExpression("^(TYT|AYT)$", ErrorMessage = "Deneme türü TYT veya AYT olmalıdır")]
        public string ExamType { get; set; } = null!;

        [Required]
        public DateTime ExamDate { get; set; }

        public int? DurationMinutes { get; set; }

        public string? Notes { get; set; }

        [Required]
        [MinLength(1)]
        public List<SubjectScoreRequest> SubjectScores { get; set; } = new();
    }
}

























