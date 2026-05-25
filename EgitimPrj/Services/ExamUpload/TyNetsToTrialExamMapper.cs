using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EgitimPrj.Models.Request;
using EgitimPrj.Models.Response;

namespace EgitimPrj.Services.ExamUpload
{
    /// <summary>
    /// PDF'teki dört TYT alan netini, Subject API'den gelen TYT derslerine dağıtır ve
    /// <see cref="CreateTrialExamRequest"/> üretir (AddStudentTrialExam ile uyumlu).
    /// </summary>
    public static class TyNetsToTrialExamMapper
    {
        public static CreateTrialExamRequest BuildRequest(
            string examName,
            DateTime examDate,
            double turkce,
            double matematik,
            double fen,
            double sosyal,
            IReadOnlyList<SubjectDto> tytSubjects)
        {
            var scores = new List<SubjectScoreRequest>();
            foreach (var s in tytSubjects)
            {
                var maxQ = s.MaxQuestions ?? 40;
                var net = PickNetForSubject(s.Name, turkce, matematik, fen, sosyal, tytSubjects);
                if (net <= 0 && turkce + matematik + fen + sosyal <= 0)
                    continue;

                var (c, w, e) = ApproximateScores(net, maxQ);
                scores.Add(new SubjectScoreRequest
                {
                    SubjectId = s.Id,
                    CorrectAnswers = c,
                    WrongAnswers = w,
                    EmptyAnswers = e
                });
            }

            return new CreateTrialExamRequest
            {
                Name = examName,
                ExamType = "TYT",
                ExamDate = examDate,
                DurationMinutes = null,
                Notes = $"PDF toplu: Tür={turkce.ToString("0.##", CultureInfo.InvariantCulture)} Mat={matematik.ToString("0.##", CultureInfo.InvariantCulture)} Fen={fen.ToString("0.##", CultureInfo.InvariantCulture)} Sos={sosyal.ToString("0.##", CultureInfo.InvariantCulture)}",
                SubjectScores = scores
            };
        }

        private static double PickNetForSubject(
            string subjectName,
            double turkce,
            double mat,
            double fen,
            double sosyal,
            IReadOnlyList<SubjectDto> allSubjects)
        {
            var n = subjectName.ToLower(new CultureInfo("tr-TR"));

            if (ContainsAny(n, "türk", "turk"))
                return turkce;
            if (ContainsAny(n, "mat"))
                return mat;

            if (IsFenSubject(n))
            {
                var fenCount = allSubjects.Count(s => IsFenSubject(s.Name.ToLower(new CultureInfo("tr-TR"))));
                return fenCount > 0 ? fen / fenCount : 0;
            }

            if (IsSosyalSubject(n))
            {
                var sCount = allSubjects.Count(s => IsSosyalSubject(s.Name.ToLower(new CultureInfo("tr-TR"))));
                return sCount > 0 ? sosyal / sCount : 0;
            }

            return 0;
        }

        private static bool IsFenSubject(string nLower) =>
            ContainsAny(nLower, "fizik", "kimya", "biyoloji", "fen");

        private static bool IsSosyalSubject(string nLower) =>
            ContainsAny(nLower, "tarih", "coğrafya", "cografya", "felsefe", "din", "inkılap", "inkilap", "vatandaş", "vatandas", "sosyal");

        private static bool ContainsAny(string haystack, params string[] needles) =>
            needles.Any(haystack.Contains);

        /// <summary>Net skoru doğru/yanlış/boş ile yaklaşık temsil eder.</summary>
        public static (int correct, int wrong, int empty) ApproximateScores(double net, int maxQ)
        {
            net = Math.Clamp(net, 0, maxQ);
            var bestC = 0;
            var bestW = 0;
            var bestDiff = double.MaxValue;

            for (var w = 0; w <= maxQ; w++)
            {
                var cFloat = net + w / 4.0;
                var c = (int)Math.Round(cFloat);
                if (c < 0 || c > maxQ)
                    continue;
                var e = maxQ - c - w;
                if (e < 0)
                    continue;
                var n = c - w / 4.0;
                var diff = Math.Abs(n - net);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    bestC = c;
                    bestW = w;
                }
            }

            var empty = maxQ - bestC - bestW;
            if (empty < 0)
            {
                bestW = Math.Max(0, bestW + empty);
                empty = maxQ - bestC - bestW;
            }

            return (bestC, bestW, Math.Max(0, empty));
        }
    }
}
