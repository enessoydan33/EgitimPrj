namespace EgitimPrj.Models.Response.Gamification
{
    public class StudyStreakDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int CurrentDays { get; set; }
        public bool IsActive { get; set; }
    }
}
