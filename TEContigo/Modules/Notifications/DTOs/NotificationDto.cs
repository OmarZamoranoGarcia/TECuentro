namespace TEContigo.Modules.Notifications.DTOs
{
    public class NotificationDto
    {
        public long Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string ReferenceType { get; set; } = string.Empty;
        public long ReferenceId { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
