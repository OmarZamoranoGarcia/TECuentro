using TEContigo.Modules.FoundItems.DTOs;
using TEContigo.Modules.LostItems.DTOs;

namespace TEContigo.Modules.Matches.DTOs
{
    public class MatchDto
    {
        public long Id { get; set; }
        public decimal MatchPercentage { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool LostUserChatRequest { get; set; }
        public bool FoundUserChatRequest { get; set; }
        public bool LostUserReturnConfirmed { get; set; }
        public bool FoundUserReturnConfirmed { get; set; }
        public LostItemDto LostItem { get; set; } = null!;
        public FoundItemDto FoundItem { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
