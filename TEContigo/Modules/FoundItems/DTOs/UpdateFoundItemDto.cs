namespace TEContigo.Modules.FoundItems.DTOs
{
    public class UpdateFoundItemDto
    {
        public string Category { get; set; } = string.Empty;
        public string Article { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public IFormFile? Photo { get; set; }
        public string? Status { get; set; }
    }
}
