using System.Collections.Generic;

namespace EgitimPrj.Models
{
    public class GenerateScheduleRequestDto
    {
        public int? TargetWeeklyHours { get; set; }
        public List<DailyAvailabilityDto> WeeklyAvailability { get; set; } = new();
        public List<SubjectPreferenceDto> SubjectPreferences { get; set; } = new();
        public List<string> WeakTopics { get; set; } = new();
        public string ExamType { get; set; } = "TYT";
    }

    public class DailyAvailabilityDto
    {
        public int DayOfWeek { get; set; }
        public List<string> TimeSlots { get; set; } = new();
    }

    public class SubjectPreferenceDto
    {
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = "";
        public List<string> SelectedTopics { get; set; } = new();
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
}
