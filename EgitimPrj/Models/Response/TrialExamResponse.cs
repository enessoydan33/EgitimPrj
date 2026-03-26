using System;
using System.Collections.Generic;

namespace EgitimPrj.Models.Response
{
    public class SubjectScoreDto
    {
        public int Id { get; set; }
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = null!;
        public string SubjectShortName { get; set; } = null!;
        public int CorrectAnswers { get; set; }
        public int WrongAnswers { get; set; }
        public int EmptyAnswers { get; set; }
        public double NetScore { get; set; }
    }

    public class TrialExamDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string ExamType { get; set; } = null!;
        public DateTime ExamDate { get; set; }
        public double TotalNet { get; set; }
        public int? Ranking { get; set; }
        public int? DurationMinutes { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<SubjectScoreDto> SubjectScores { get; set; } = new();
    }

    public class TrialExamListItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string ExamType { get; set; } = null!;
        public DateTime ExamDate { get; set; }
        public double TotalNet { get; set; }
        public double? NetChange { get; set; }
    }

    public class TrialExamStatsDto
    {
        public int TotalTrials { get; set; }
        public double AverageNet { get; set; }
        public double HighestNet { get; set; }
        public double? LastChange { get; set; }
        public DateTime? LastTrialDate { get; set; }
    }

    public class TrialExamListResponse
    {
        public List<TrialExamListItemDto> Trials { get; set; } = new();
        public TrialExamStatsDto Stats { get; set; } = new();
    }
}
























