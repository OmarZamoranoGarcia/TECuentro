namespace TEContigo.Modules.Conversations.Models
{
    public class ConversationsModel
    {
        public long Id { get; set; }
        public long? MatchId { get; set; }
        public long? ContactRequestId { get; set; }
        public bool RequesterConfirmedReturn { get; set; }
        public bool FinderConfirmedReturn { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
    }
}
