namespace EgitimPrj.Models.ExamUpload
{
    public class TeacherPanelStudentInfo
    {
        public int StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? StudentNumber { get; set; }
        public int ClassroomId { get; set; }
    }
}
