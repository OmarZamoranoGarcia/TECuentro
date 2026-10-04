namespace TEContigo.Modules.ContactRequests.Models
{
    public class ContactRequestsModel
    {
        public long Id { get; set; }
        public long FoundItemId { get; set; }
        public long RequesterId { get; set; }
        public long? MatchId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
    }
}
