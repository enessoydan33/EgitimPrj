using System;
using System.Collections.Generic;

namespace EgitimPrj.Models.Response
{
    public class PomodoroSessionDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public int? ScheduleId { get; set; }
        public string SessionType { get; set; } = null!;
        public int PlannedDurationMinutes { get; set; }
        public int? ActualDurationMinutes { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool IsCompleted { get; set; }
        public string? Notes { get; set; }
        public int XpEarned { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PomodoroStatsDto
    {
        public int TotalSessions { get; set; }
        public int CompletedSessions { get; set; }
        public int TotalMinutesStudied { get; set; }
        public int TotalXpEarned { get; set; }
        public int TodayMinutes { get; set; }
        public int TodaySessions { get; set; }
        public int WeekMinutes { get; set; }
        public double AverageSessionMinutes { get; set; }
    }

    public class StudyScheduleDto
    {
        public int Id { get; set; }
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = null!;
        public string SubjectShortName { get; set; } = null!;
        public string? SubjectColorHex { get; set; }
        public int DayOfWeek { get; set; }
        public string DayName { get; set; } = null!;
        public string StartTime { get; set; } = null!;
        public string EndTime { get; set; } = null!;
        public int DurationMinutes { get; set; }
        public string? Topic { get; set; }
        public bool IsActive { get; set; }
    }

    public class DailyScheduleDto
    {
        public int DayOfWeek { get; set; }
        public string DayName { get; set; } = null!;
        public List<StudyScheduleDto> Schedules { get; set; } = new();
        public int TotalMinutes { get; set; }
    }
}
























