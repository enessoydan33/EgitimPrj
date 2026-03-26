using System.ComponentModel.DataAnnotations;

namespace EgitimPrj.Models.Request
{
    public class StartPomodoroRequest
    {
        public int? SubjectId { get; set; }
        public int? ScheduleId { get; set; }

        [Required]
        [Range(1, 120)]
        public int PlannedDurationMinutes { get; set; } = 25;

        [RegularExpression("^(STUDY|SHORT_BREAK|LONG_BREAK)$")]
        public string SessionType { get; set; } = "STUDY";

        public string? Notes { get; set; }
    }

    public class CompletePomodoroRequest
    {
        public int? ActualDurationMinutes { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateScheduleRequest
    {
        [Required]
        public int SubjectId { get; set; }

        [Required]
        [Range(0, 6)]
        public int DayOfWeek { get; set; }

        [Required]
        public string StartTime { get; set; } = null!;

        [Required]
        public string EndTime { get; set; } = null!;

        public string? Topic { get; set; }
    }

    public class UpdateScheduleRequest
    {
        public int? SubjectId { get; set; }
        public int? DayOfWeek { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public string? Topic { get; set; }
        public bool? IsActive { get; set; }
    }
}
























