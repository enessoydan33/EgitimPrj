namespace EgitimPrj.Models.Response.Gamification
{
    public class AchievementDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Icon { get; set; }

        public int? XpRequirement { get; set; }
        public int? StreakRequirement { get; set; }
        public int? StudyHoursRequirement { get; set; }

        // Status
        public string? BadgeColor { get; set; }
        public bool IsEarned { get; set; }
        public DateTime? EarnedAt { get; set; }
    }
}
