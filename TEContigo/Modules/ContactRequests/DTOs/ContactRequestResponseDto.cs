namespace TEContigo.Modules.ContactRequests.DTOs
{
    public class ContactRequestResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public long? Id { get; set; }
    }
}
