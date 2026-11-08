namespace TEContigo.Modules.Notifications.Models
{
    public class NotificationsModel
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string ReferenceType { get; set; } = string.Empty;
        public long ReferenceId { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
