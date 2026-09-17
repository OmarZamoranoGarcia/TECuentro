namespace TEContigo.Modules.Reports.DTOs
{
    public class UpdateReportDto
    {
        public string? Name { get; set; }

        public string? Category { get; set; }

        public string? Description { get; set; }

        public string? Status { get; set; }

        public IFormFile? Photo { get; set; }
    }
}
