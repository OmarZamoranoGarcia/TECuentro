namespace TEContigo.Modules.FoundItems.DTOs
{
    public class FoundItemResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public long? Id { get; set; }
    }
}
