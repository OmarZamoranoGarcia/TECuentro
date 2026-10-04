namespace TEContigo.Modules.Messages.DTOs
{
    public class MessageResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public long? Id { get; set; }
    }
}
