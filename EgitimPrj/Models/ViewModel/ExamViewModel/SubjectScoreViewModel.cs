namespace EgitimPrj.Models.ViewModel.ExamViewModel
{
    public class SubjectScoreViewModel
    {
        public int? Id { get; set; } // Edit modunda gelir, Create modunda null

        public int SubjectId { get; set; }
        public string? SubjectName { get; set; } // Details/Edit görüntü için

        public int CorrectAnswers { get; set; }
        public int WrongAnswers { get; set; }
        public int EmptyAnswers { get; set; }

        public double NetScore { get; set; } // Sadece görüntü için
    }
}
