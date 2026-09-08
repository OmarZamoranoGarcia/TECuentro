namespace TEContigo.Modules.Reports.DTOs
{
    public class ReportResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public long? Id { get; set; }
    }
}
