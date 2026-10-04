using TEContigo.Modules.FoundItems.DTOs;

namespace TEContigo.Modules.ContactRequests.DTOs
{
    public class ContactRequestDto
    {
        public long Id { get; set; }
        public long RequesterId { get; set; }
        public long? MatchId { get; set; }
        public string Status { get; set; } = string.Empty;
        public FoundItemDto FoundItem { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
    }
}
