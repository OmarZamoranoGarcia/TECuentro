namespace TEContigo.Modules.Reports.DTOs
{
    public class UpdateReportDto
    {
        public string Name { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string? PhotoPath { get; set; }

        public string Description { get; set; } = string.Empty;

        public string? Status { get; set; }
    }
}
