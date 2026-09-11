namespace TEContigo.Modules.LostItems.DTOs
{
    public class UpdateLostItemDto
    {
        public string Category { get; set; } = string.Empty;
        public string Article { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? PhotoPath { get; set; }
        public string? Status { get; set; }
    }
}
