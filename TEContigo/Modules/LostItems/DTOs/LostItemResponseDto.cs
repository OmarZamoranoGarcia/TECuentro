namespace TEContigo.Modules.LostItems.DTOs
{
    public class LostItemResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public long? Id { get; set; }
    }
}
