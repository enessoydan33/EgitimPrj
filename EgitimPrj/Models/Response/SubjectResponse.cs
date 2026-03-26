namespace EgitimPrj.Models.Response
{
    public class SubjectDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string ShortName { get; set; } = null!;
        public string? ColorHex { get; set; }
        public string? ExamType { get; set; }
        public int? MaxQuestions { get; set; }
        public bool IsActive { get; set; }
    }
}



















