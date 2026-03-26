namespace EgitimPrj.Models.Response.Gamification
{
    public class CurrentStreakStatusDto
    {
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public bool HasStudiedToday { get; set; }
        public List<StudyStreakDto> StreakHistory { get; set; } = new();
    }
}
