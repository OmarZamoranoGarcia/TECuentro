using TEContigo.Modules.FoundItems.DTOs;
using TEContigo.Modules.LostItems.DTOs;

namespace TEContigo.Modules.Conversations.DTOs
{
    public class ConversationDto
    {
        public long Id { get; set; }
        public long? MatchId { get; set; }
        public long? ContactRequestId { get; set; }

        public long RequesterId { get; set; }
        public long FinderId { get; set; }

        public bool RequesterConfirmedReturn { get; set; }
        public bool FinderConfirmedReturn { get; set; }

        /// <summary>
        /// Siempre presente: en ambos orígenes (Match o ContactRequest)
        /// hay un FoundItem de por medio.
        /// </summary>
        public FoundItemDto FoundItem { get; set; } = null!;

        /// <summary>
        /// Solo presente cuando la conversación viene de un Match. Cuando
        /// viene de un ContactRequest, el requester nunca publicó un
        /// LostItem, así que este campo queda null.
        /// </summary>
        public LostItemDto? LostItem { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
    }
}
