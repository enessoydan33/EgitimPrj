using System.Globalization;
using EgitimPrj.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EgitimPrj.Services;

public static class TeacherHomeworkDocumentationPdf
{
    public static byte[] BuildGuide()
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10.5f).LineHeight(1.35f));

                page.Header().Column(col =>
                {
                    col.Item().Text("Öğretmen paneli — ödev modülü kılavuzu")
                        .FontSize(18).SemiBold().FontColor(Colors.Blue.Darken3);
                    col.Item().PaddingTop(4).Text($"Bu belge {DateTime.Now:dd.MM.yyyy} tarihinde oluşturulmuştur.")
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                });

                page.Content().PaddingTop(16).Column(col =>
                {
                    col.Spacing(14);

                    Section(col, "1. Ödev gönderme",
                        "Gönderim hedefi olarak sınıf geneli veya tekil öğrenci seçin.",
                        "Ders, konu/başlık ve son teslim tarihi zorunludur. İsteğe bağlı açıklama alanına öğrenciye yönelik ek notlar yazabilirsiniz.",
                        "Sınıf listesi ve öğrenciler, bağlı olduğunuz hesaptaki sınıflar üzerinden yüklenir; gönderim arka plandaki öğretmen paneli API’sine kaydedilir.");

                    Section(col, "2. Gönderilen ödevler listesi",
                        "Tabloda her ödev için hedef (sınıf veya öğrenci), ders, konu, teslim tarihi ve kısa açıklama görüntülenir.",
                        "Kontrol sütunundaki düğmeyle ilgili ödev için öğrenci tamamlama işaretlemesi yapılır.");

                    Section(col, "3. Ödev kontrolü",
                        "Kontrol düğmesine tıkladığınızda açılan pencerede, hedef sınıfın öğrencileri veya tekil öğrenci listelenir.",
                        "Ödevi teslim eden veya tamamlayan öğrencileri işaretleyip Kaydet ile kaydedin.",
                        "Bu kontrol listesi tarayıcınızda yerel olarak saklanır; farklı cihaz veya tarayıcıda aynı işaretlemeler görünmeyebilir. Kalıcı kayıt için kurumunuzun kullandığı resmi sistem varsa orayı da güncellemeniz önerilir.");

                    Section(col, "4. PDF belgeleri",
                        "Ödev kontrol penceresinde “Kılavuz (PDF)” ile bu modülün kullanımını özetleyen belgeyi açabilirsiniz.",
                        "“Bu ödevin özeti (PDF)” ile konu, ders, teslim, hedef, açıklama ve kontrol penceresindeki işaretlere göre tamamlayan / tamamlamayan öğrenci listesini indirebilir veya yazdırabilirsiniz.");
                });

                page.Footer().AlignCenter().Text(txt =>
                {
                    txt.Span("CoMentor / Eğitim paneli — ödev yönetimi").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf();
    }

    public static byte[] BuildSnapshot(HomeworkSnapshotRequest body)
    {
        var topic = string.IsNullOrWhiteSpace(body.Topic) ? "—" : body.Topic.Trim();
        var subject = string.IsNullOrWhiteSpace(body.Subject) ? "—" : body.Subject.Trim();
        var dueDisplay = string.IsNullOrWhiteSpace(body.DueDate) ? "—" : body.DueDate.Trim();
        if (!string.IsNullOrWhiteSpace(body.DueDate)
            && DateTime.TryParse(body.DueDate, null, DateTimeStyles.RoundtripKind, out var parsedDue))
        {
            var local = parsedDue.Kind == DateTimeKind.Utc ? parsedDue.ToLocalTime() : parsedDue;
            dueDisplay = local.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR"));
        }

        var description = string.IsNullOrWhiteSpace(body.Description) ? "—" : body.Description.Trim();
        var target = string.IsNullOrWhiteSpace(body.Target) ? "—" : body.Target.Trim();

        var students = body.Students ?? new List<HomeworkSnapshotStudentStatus>();
        var named = students
            .Where(s => !string.IsNullOrWhiteSpace(s.Name))
            .Select(s => new HomeworkSnapshotStudentStatus { Name = s.Name.Trim(), Completed = s.Completed })
            .ToList();
        var done = named.Where(s => s.Completed).OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ToList();
        var notDone = named.Where(s => !s.Completed).OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11).LineHeight(1.4f));

                page.Header().Column(col =>
                {
                    col.Item().Text("Ödev özeti (öğretmen)")
                        .FontSize(17).SemiBold().FontColor(Colors.Blue.Darken3);
                    col.Item().PaddingTop(6).Text($"Oluşturulma: {DateTime.Now:dd.MM.yyyy HH:mm}")
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                });

                page.Content().PaddingTop(20).Column(col =>
                {
                    col.Spacing(10);
                    FieldRow(col, "Konu / başlık", topic);
                    FieldRow(col, "Ders", subject);
                    FieldRow(col, "Son teslim", dueDisplay);
                    FieldRow(col, "Hedef", target);
                    col.Item().PaddingTop(8).Text("Açıklama").Bold().FontSize(12);
                    col.Item().Background(Colors.Grey.Lighten4).Padding(12).Text(description)
                        .FontSize(10.5f);

                    StudentStatusBlock(col, done, notDone);
                });

                page.Footer().AlignCenter().PaddingTop(24).Text(
                    "Öğrenci işaretleri, PDF oluşturulduğu andaki kontrol penceresine göredir. Kaydet ile kalıcılaştırılmış olmayabilir. Resmi kayıt için kurum süreçlerinizi kullanın.")
                    .FontSize(8).FontColor(Colors.Grey.Medium).Italic();
            });
        }).GeneratePdf();
    }

    private static void StudentStatusBlock(
        ColumnDescriptor col,
        IReadOnlyList<HomeworkSnapshotStudentStatus> completed,
        IReadOnlyList<HomeworkSnapshotStudentStatus> notCompleted)
    {
        col.Item().PaddingTop(14).Text("Öğrenci durumu").Bold().FontSize(12);

        if (completed.Count == 0 && notCompleted.Count == 0)
        {
            col.Item().Text("Öğrenci listesi bu PDF ile iletilmedi veya henüz yüklenmedi.")
                .FontSize(10).FontColor(Colors.Grey.Darken1).Italic();
            return;
        }

        col.Item().PaddingTop(4).Text($"Tamamlayan ({completed.Count})").SemiBold().FontSize(10.5f);
        if (completed.Count == 0)
            col.Item().PaddingLeft(10).Text("—").FontSize(10).FontColor(Colors.Grey.Medium);
        else
            foreach (var s in completed)
                col.Item().PaddingLeft(10).Text("• " + s.Name).FontSize(10);

        col.Item().PaddingTop(10).Text($"Tamamlamayan ({notCompleted.Count})").SemiBold().FontSize(10.5f);
        if (notCompleted.Count == 0)
            col.Item().PaddingLeft(10).Text("—").FontSize(10).FontColor(Colors.Grey.Medium);
        else
            foreach (var s in notCompleted)
                col.Item().PaddingLeft(10).Text("• " + s.Name).FontSize(10);
    }

    private static void Section(ColumnDescriptor col, string title, params string[] paragraphs)
    {
        col.Item().Text(title).Bold().FontSize(13).FontColor(Colors.Blue.Darken2);
        foreach (var p in paragraphs)
            col.Item().Text(p).FontSize(10.5f);
    }

    private static void FieldRow(ColumnDescriptor col, string label, string value)
    {
        col.Item().Row(row =>
        {
            row.RelativeItem(2).Text(label + ":").SemiBold();
            row.RelativeItem(5).Text(value);
        });
    }
}
