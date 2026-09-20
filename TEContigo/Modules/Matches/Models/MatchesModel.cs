namespace TEContigo.Modules.Matches.Models
{
    public class MatchesModel
    {
        public long Id { get; set; }
        public long LostItemId { get; set; }
        public long FoundItemId { get; set; }
        public decimal MatchPercentage { get; set; }
        public bool LostUserChatRequest { get; set; }
        public bool FoundUserChatRequest { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool LostUserReturnConfirmed { get; set; }
        public bool FoundUserReturnConfirmed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
