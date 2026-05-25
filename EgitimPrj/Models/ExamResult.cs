using System;

namespace EgitimPrj.Models
{
    /// <summary>
    /// PDF veya API akışından gelen tek bir öğrenci deneme satırı.
    /// Eşleşme öncesi <see cref="StudentId"/> null olabilir.
    /// </summary>
    public class ExamResult
    {
        public int? StudentId { get; set; }

        /// <summary>PDF / rapor üzerindeki ad soyad.</summary>
        public string StudentName { get; set; } = string.Empty;

        /// <summary>Varsa okul / öğrenci numarası (PDF veya API).</summary>
        public string? StudentNumber { get; set; }

        public double TurkishNet { get; set; }
        public double MathNet { get; set; }
        public double ScienceNet { get; set; }
        public double SocialNet { get; set; }
        public double TotalNet { get; set; }

        /// <summary>PDF tablosunda D-Y-N biçimindeyse doldurulur (klasik tek satır net listelerinde null).</summary>
        public double? TurkishCorrect { get; set; }
        public double? TurkishWrong { get; set; }
        public double? SocialCorrect { get; set; }
        public double? SocialWrong { get; set; }
        public double? MathCorrect { get; set; }
        public double? MathWrong { get; set; }
        public double? ScienceCorrect { get; set; }
        public double? ScienceWrong { get; set; }
        public double? TotalCorrect { get; set; }
        public double? TotalWrong { get; set; }

        public DateTime ExamDate { get; set; }

        /// <summary>Kaydetme için TeacherPanel sınıf kimliği (eşleşince dolu).</summary>
        public int? ClassroomId { get; set; }
    }
}
