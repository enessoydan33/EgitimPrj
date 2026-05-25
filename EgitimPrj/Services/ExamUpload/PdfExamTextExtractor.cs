using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace EgitimPrj.Services.ExamUpload
{
    public interface IPdfExamTextExtractor
    {
        /// <summary>PDF'den satır listesi (Y koordinatına göre gruplanmış metin).</summary>
        IReadOnlyList<string> ExtractLines(Stream pdfStream);
    }

    public sealed class PdfExamTextExtractor : IPdfExamTextExtractor
    {
        private const double LineTolerance = 3.0;

        public IReadOnlyList<string> ExtractLines(Stream pdfStream)
        {
            using var document = PdfDocument.Open(pdfStream);
            var lines = new List<string>();
            foreach (var page in document.GetPages())
            {
                var words = page.GetWords().ToList();
                if (words.Count == 0)
                    continue;

                var grouped = words
                    .GroupBy(w => System.Math.Round(w.BoundingBox.BottomLeft.Y / LineTolerance) * LineTolerance)
                    .OrderByDescending(g => g.Key);

                foreach (var group in grouped)
                {
                    var line = string.Join(
                        " ",
                        group.OrderBy(w => w.BoundingBox.BottomLeft.X).Select(w => w.Text));
                    if (!string.IsNullOrWhiteSpace(line))
                        lines.Add(line.Trim());
                }
            }

            return lines;
        }

        public static string Preview(IReadOnlyList<string> lines, int maxChars = 4000)
        {
            var sb = new StringBuilder();
            foreach (var line in lines)
            {
                if (sb.Length >= maxChars)
                    break;
                sb.AppendLine(line);
            }

            return sb.Length > maxChars ? sb.ToString(0, maxChars) + "…" : sb.ToString();
        }
    }
}
