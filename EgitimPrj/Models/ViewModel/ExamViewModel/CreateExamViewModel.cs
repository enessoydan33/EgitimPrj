using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace EgitimPrj.Models.ViewModel.ExamViewModel
{
    public class CreateExamViewModel
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
            public List<SubjectScoreViewModel> SubjectScores { get; set; } = new();
        

    }
}
