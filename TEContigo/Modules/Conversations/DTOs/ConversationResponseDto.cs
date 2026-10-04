namespace TEContigo.Modules.Conversations.DTOs
{
    public class ConversationResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public long? Id { get; set; }
        public bool? BothConfirmed { get; set; }
    }
}
