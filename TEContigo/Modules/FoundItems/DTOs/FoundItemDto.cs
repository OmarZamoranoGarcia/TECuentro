namespace TEContigo.Modules.FoundItems.DTOs
{
    public class FoundItemDto
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Article { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
