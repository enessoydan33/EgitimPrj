using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using EgitimPrj.Models;

namespace EgitimPrj.Services.ExamUpload
{
    public interface IPdfExamResultLineParser
    {
        IReadOnlyList<ExamResult> ParseLines(IEnumerable<string> lines, DateTime examDate);
    }

    /// <summary>
    /// Türkçe deneme çıktılarında sık görülen satır biçimlerini dener:
    /// (1) Okul TYT listeleri: sıra, öğrenci no, ad, sınıf; her ders için D-Y-N üçlüsü (TYT Türkçe → Sosyal → Mat → Fen)
    ///     ve Toplam TYT üçlüsü; netler üçlünün üçüncü sütunundadır.
    /// (2) Klasik: [No] Ad Soyad — satır sonunda dört net + toplam (veya dört net).
    /// </summary>
    public sealed class PdfExamResultLineParser : IPdfExamResultLineParser
    {
        private static readonly Regex NumberToken = new(
            @"(?<n>-?\d+[.,]\d+|-?\d+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly HashSet<string> SkipPrefixes = new(StringComparer.OrdinalIgnoreCase)
        {
            "sıra", "sira", "no", "ad", "soyad", "öğrenci", "ogrenci", "türkçe", "turkce",
            "matematik", "fen", "sosyal", "toplam", "tyt", "ayt", "ders", "net", "sonuç",
            "sonuc", "isim", "numara", "şube", "sube", "sınıf", "sinif"
        };

        private static readonly Regex GluedClassSuffix = new(
            @"(\S+?)(\d{1,2}-[A-Za-zğüşıöçĞÜŞİÖÇ])$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public IReadOnlyList<ExamResult> ParseLines(IEnumerable<string> lines, DateTime examDate)
        {
            var list = new List<ExamResult>();
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.Length < 8)
                    continue;

                if (ShouldSkipLine(line))
                    continue;

                var firstWord = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
                if (SkipPrefixes.Any(p => line.StartsWith(p + " ", StringComparison.OrdinalIgnoreCase)
                                          || string.Equals(firstWord, p, StringComparison.OrdinalIgnoreCase)))
                    continue;

                if (!TryParseDataLine(line, examDate, out var row) || row is null)
                    continue;

                list.Add(row);
            }

            return list;
        }

        /// <summary>Bilgi Sarmalı / okul TYT PDF üst bilgi ve özet satırlarını atlar.</summary>
        private static bool ShouldSkipLine(string line)
        {
            var lower = line.ToLowerInvariant();
            if (lower.StartsWith("okul ", StringComparison.OrdinalIgnoreCase) && lower.Contains("tyt"))
                return true;
            if (lower.StartsWith("katılım", StringComparison.OrdinalIgnoreCase))
                return true;
            if (lower.Contains("il ilçe okul", StringComparison.OrdinalIgnoreCase)
                || lower.Contains("il ilce okul", StringComparison.OrdinalIgnoreCase))
                return true;
            if (lower.Contains("ortalaması", StringComparison.OrdinalIgnoreCase)
                || lower.Contains("ortalamasi", StringComparison.OrdinalIgnoreCase))
                return true;
            if (lower.Contains("genel ortalama", StringComparison.OrdinalIgnoreCase))
                return true;
            if (Regex.IsMatch(line, @"^\d+\s*/\s*\d+$"))
                return true;
            if (lower.StartsWith("d y n", StringComparison.OrdinalIgnoreCase))
                return true;
            // Üst üste D-Y-N başlığı (tek satırda tekrarlı)
            if (Regex.IsMatch(line, @"^D\s+Y\s+N(\s+D\s+Y\s+N){3,}", RegexOptions.IgnoreCase))
                return true;

            return false;
        }

        private static bool TryParseDataLine(string line, DateTime examDate, out ExamResult? row)
        {
            row = null;
            var matches = NumberToken.Matches(line);
            if (matches.Count < 4)
                return false;

            var numbers = new List<double>(matches.Count);
            foreach (Match m in matches)
            {
                if (!TryParseDouble(m.Groups["n"].Value, out var v))
                    return false;
                numbers.Add(v);
            }

            if (TryParseTyDyNTripletFormat(numbers, line, examDate, out row))
                return true;

            // Uzun tablo satırı üçlü biçime uymuyorsa eski "son 5 sayı" kuralına düşme (sıra sütunları yanlış net olur).
            if (numbers.Count >= 18)
                return false;

            return TryParseLegacyNetTailFormat(numbers, line, examDate, out row);
        }

        /// <summary>
        /// TYT Türkçe / Sosyal / Mat / Fen / Toplam sütunlarında her biri (D,Y,N) olan tablolar.
        /// Sıra + öğrenci no sonrası isteğe bağlı sayısal sınıf (9–13); ardından 5× (D,Y,N).
        /// Netler her üçlünün üçüncü değeridir.
        /// </summary>
        private static bool TryParseTyDyNTripletFormat(
            IReadOnlyList<double> numbers,
            string line,
            DateTime examDate,
            out ExamResult? row)
        {
            row = null;
            ExamResult? best = null;
            var bestError = double.MaxValue;

            foreach (var dataStart in new[] { 3, 2 })
            {
                if (!TryParseTyDyNTripletAt(numbers, line, examDate, dataStart, out var candidate) || candidate is null)
                    continue;

                var err = Math.Abs(
                    candidate.TurkishNet + candidate.SocialNet + candidate.MathNet + candidate.ScienceNet
                    - candidate.TotalNet);

                if (err < bestError)
                {
                    bestError = err;
                    best = candidate;
                }
            }

            row = best;
            return row is not null;
        }

        private static bool TryParseTyDyNTripletAt(
            IReadOnlyList<double> numbers,
            string line,
            DateTime examDate,
            int dataStart,
            out ExamResult? row)
        {
            row = null;
            var lastNetIndex = dataStart + 14;
            if (numbers.Count <= lastNetIndex)
                return false;

            if (dataStart == 3 && !LooksLikeNumericClass(numbers[2]))
                return false;

            var tur = numbers[dataStart + 2];
            var sos = numbers[dataStart + 5];
            var mat = numbers[dataStart + 8];
            var fen = numbers[dataStart + 11];
            var total = numbers[dataStart + 14];

            foreach (var x in new[] { tur, sos, mat, fen })
            {
                if (double.IsNaN(x) || x < -1 || x > 130)
                    return false;
            }

            if (double.IsNaN(total) || total < -1 || total > 130)
                return false;

            var rank = numbers[0];
            if (rank < 1 || rank > 200_000 || Math.Abs(rank - Math.Round(rank)) > 0.001)
                return false;

            var ogNo = numbers[1];
            if (ogNo < 1 || ogNo >= 10_000_000 || Math.Abs(ogNo - Math.Round(ogNo)) > 0.001)
                return false;

            var sumFour = tur + sos + mat + fen;
            if (Math.Abs(sumFour - total) > 1.6)
                return false;

            if (!TryBuildStudentRow(
                    line, examDate, tur, mat, fen, sos, total,
                    preferStudentNoFromNumbers: true,
                    tripletDataStart: dataStart,
                    numbers,
                    out row))
                return false;

            row!.TurkishCorrect = numbers[dataStart];
            row.TurkishWrong = numbers[dataStart + 1];
            row.SocialCorrect = numbers[dataStart + 3];
            row.SocialWrong = numbers[dataStart + 4];
            row.MathCorrect = numbers[dataStart + 6];
            row.MathWrong = numbers[dataStart + 7];
            row.ScienceCorrect = numbers[dataStart + 9];
            row.ScienceWrong = numbers[dataStart + 10];
            row.TotalCorrect = numbers[dataStart + 12];
            row.TotalWrong = numbers[dataStart + 13];

            return true;
        }

        private static bool LooksLikeNumericClass(double value)
        {
            if (Math.Abs(value - Math.Round(value)) > 0.001)
                return false;
            var grade = (int)Math.Round(value);
            return grade is >= 5 and <= 13;
        }

        private static bool TryParseLegacyNetTailFormat(
            IReadOnlyList<double> numbers,
            string line,
            DateTime examDate,
            out ExamResult? row)
        {
            row = null;
            double tur, mat, fen, sos, total;
            if (numbers.Count >= 5)
            {
                var tail = numbers.TakeLast(5).ToArray();
                tur = tail[0];
                mat = tail[1];
                fen = tail[2];
                sos = tail[3];
                total = tail[4];
            }
            else
            {
                var tail = numbers.TakeLast(4).ToArray();
                tur = tail[0];
                mat = tail[1];
                fen = tail[2];
                sos = tail[3];
                total = tur + mat + fen + sos;
            }

            return TryBuildStudentRow(
                line,
                examDate,
                tur,
                mat,
                fen,
                sos,
                total,
                preferStudentNoFromNumbers: false,
                tripletDataStart: null,
                numbers,
                out row);
        }

        private static bool TryBuildStudentRow(
            string line,
            DateTime examDate,
            double tur,
            double mat,
            double fen,
            double sos,
            double total,
            bool preferStudentNoFromNumbers,
            int? tripletDataStart,
            IReadOnlyList<double> numbers,
            out ExamResult? row)
        {
            row = null;
            string namePart;
            if (tripletDataStart.HasValue && TryExtractNameBeforeTriplet(line, tripletDataStart.Value, out var beforeTriplet))
                namePart = beforeTriplet;
            else
            {
                namePart = NumberToken.Replace(line, " ").Trim();
                namePart = Regex.Replace(namePart, @"\s+", " ");
            }

            string? studentNo = null;
            if (preferStudentNoFromNumbers && numbers.Count >= 2)
            {
                var noVal = numbers[1];
                if (noVal >= 1 && noVal < 10_000_000 && Math.Abs(noVal - Math.Round(noVal)) < 0.001)
                    studentNo = ((long)Math.Round(noVal)).ToString(CultureInfo.InvariantCulture);
            }

            var tokens = namePart.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            if (tokens.Count > 0 && Regex.IsMatch(tokens[0], @"^\d{4,12}$"))
            {
                studentNo = tokens[0];
                tokens.RemoveAt(0);
            }

            DetachGluedClassFromTokens(tokens);
            StripTrailingClassTokens(tokens);

            var name = string.Join(" ", tokens).Trim();
            if (name.Length < 3)
                return false;

            row = new ExamResult
            {
                StudentName = name,
                StudentNumber = studentNo,
                TurkishNet = tur,
                MathNet = mat,
                ScienceNet = fen,
                SocialNet = sos,
                TotalNet = total,
                ExamDate = examDate
            };
            return true;
        }

        private static bool TryParseDouble(string s, out double value)
        {
            s = s.Replace(',', '.');
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        /// <summary>Öğrenci no sonrası metin; D-Y-N sayıları başlamadan önce (sınıf dahil).</summary>
        private static bool TryExtractNameBeforeTriplet(string line, int dataStart, out string namePart)
        {
            namePart = string.Empty;
            var matches = NumberToken.Matches(line);
            if (matches.Count <= dataStart || matches.Count < 2)
                return false;

            var start = matches[1].Index + matches[1].Length;
            var end = matches[dataStart].Index;
            if (end <= start)
                return false;

            namePart = Regex.Replace(line.Substring(start, end - start).Trim(), @"\s+", " ");
            return namePart.Length > 0;
        }

        /// <summary>PDF'de soyad ile bitişik yazılan sınıf (ör. ŞENGÜL12-C).</summary>
        private static void DetachGluedClassFromTokens(List<string> tokens)
        {
            if (tokens.Count == 0)
                return;

            var last = tokens[^1];
            var m = GluedClassSuffix.Match(last);
            if (!m.Success)
                return;

            var nameBit = m.Groups[1].Value.Trim();
            if (nameBit.Length == 0)
                tokens.RemoveAt(tokens.Count - 1);
            else
                tokens[^1] = nameBit;
        }

        private static void StripTrailingClassTokens(List<string> tokens)
        {
            while (tokens.Count > 0 && IsClassToken(tokens[^1]))
                tokens.RemoveAt(tokens.Count - 1);
        }

        private static bool IsClassToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            var t = token.Trim();
            if (string.Equals(t, "MEZUN", StringComparison.OrdinalIgnoreCase))
                return true;
            if (Regex.IsMatch(t, @"^MEZUN[-/]?[A-Za-zğüşıöçĞÜŞİÖÇ]?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                return true;
            if (Regex.IsMatch(t, @"^\d{1,2}$", RegexOptions.CultureInvariant))
                return true;
            return Regex.IsMatch(
                t,
                @"^\d{1,2}[-/][A-Za-zğüşıöçĞÜŞİÖÇ]$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }
    }
}
