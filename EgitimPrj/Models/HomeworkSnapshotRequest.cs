namespace EgitimPrj.Models;

public class HomeworkSnapshotStudentStatus
{
    public string Name { get; set; } = string.Empty;
    public bool Completed { get; set; }
}

public class HomeworkSnapshotRequest
{
    public string Topic { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? DueDate { get; set; }
    public string? Description { get; set; }
    public string? Target { get; set; }

    /// <summary>Ödev kontrolündeki öğrenciler ve anlık işaret durumu.</summary>
    public List<HomeworkSnapshotStudentStatus>? Students { get; set; }
}
