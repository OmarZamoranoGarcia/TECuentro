namespace TEContigo.Modules.FoundItems.DTOs
{
    public class CreateFoundItemDto
    {
        public string Category { get; set; } = string.Empty;
        public string Article { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? PhotoPath { get; set; }
    }
}
