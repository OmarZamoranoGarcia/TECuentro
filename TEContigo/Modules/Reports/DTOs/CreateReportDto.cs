namespace TEContigo.Modules.Reports.DTOs
{
    public class CreateReportDto
    {
        public string Name { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string? PhotoPath { get; set; }

        public string Description { get; set; } = string.Empty;
    }
}
