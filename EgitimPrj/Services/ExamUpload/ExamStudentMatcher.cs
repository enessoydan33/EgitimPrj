using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EgitimPrj.Models;
using EgitimPrj.Models.ExamUpload;

namespace EgitimPrj.Services.ExamUpload
{
    public interface IExamStudentMatcher
    {
        (List<ExamResult> matched, List<ExamResult> unmatched) Match(
            IReadOnlyList<ExamResult> parsedRows,
            IReadOnlyList<TeacherPanelStudentInfo> students);
    }

    public sealed class ExamStudentMatcher : IExamStudentMatcher
    {
        public (List<ExamResult> matched, List<ExamResult> unmatched) Match(
            IReadOnlyList<ExamResult> parsedRows,
            IReadOnlyList<TeacherPanelStudentInfo> students)
        {
            var matched = new List<ExamResult>();
            var unmatched = new List<ExamResult>();

            var byNumber = students
                .Where(s => !string.IsNullOrWhiteSpace(s.StudentNumber))
                .GroupBy(s => NormalizeNumber(s.StudentNumber!))
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

            foreach (var row in parsedRows)
            {
                TeacherPanelStudentInfo? hit = null;

                if (!string.IsNullOrWhiteSpace(row.StudentNumber))
                {
                    var key = NormalizeNumber(row.StudentNumber);
                    byNumber.TryGetValue(key, out hit);
                }

                if (hit is null)
                {
                    var rn = NormalizeName(row.StudentName);
                    hit = students.FirstOrDefault(s => NormalizeName(s.FullName) == rn);
                }

                if (hit is null)
                {
                    unmatched.Add(Clone(row));
                    continue;
                }

                var m = Clone(row);
                m.StudentId = hit.StudentId;
                m.ClassroomId = hit.ClassroomId;
                matched.Add(m);
            }

            return (matched, unmatched);
        }

        private static ExamResult Clone(ExamResult r) => new()
        {
            StudentId = r.StudentId,
            StudentName = r.StudentName,
            StudentNumber = r.StudentNumber,
            TurkishNet = r.TurkishNet,
            MathNet = r.MathNet,
            ScienceNet = r.ScienceNet,
            SocialNet = r.SocialNet,
            TotalNet = r.TotalNet,
            TurkishCorrect = r.TurkishCorrect,
            TurkishWrong = r.TurkishWrong,
            SocialCorrect = r.SocialCorrect,
            SocialWrong = r.SocialWrong,
            MathCorrect = r.MathCorrect,
            MathWrong = r.MathWrong,
            ScienceCorrect = r.ScienceCorrect,
            ScienceWrong = r.ScienceWrong,
            TotalCorrect = r.TotalCorrect,
            TotalWrong = r.TotalWrong,
            ExamDate = r.ExamDate,
            ClassroomId = r.ClassroomId
        };

        private static string NormalizeNumber(string n)
        {
            var d = new string(n.Where(char.IsDigit).ToArray());
            return d.TrimStart('0');
        }

        private static string NormalizeName(string name)
        {
            var t = name.Trim().ToLower(new CultureInfo("tr-TR"));
            t = string.Join(" ", t.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            return t;
        }
    }
}
