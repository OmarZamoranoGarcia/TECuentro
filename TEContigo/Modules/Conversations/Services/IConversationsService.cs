using TEContigo.Modules.Conversations.DTOs;

namespace TEContigo.Modules.Conversations.Services
{
    public interface IConversationsService
    {
        Task<IEnumerable<ConversationDto>> GetAllForCurrentUserAsync();
        Task<ConversationDto?> GetByIdAsync(long id);

        /// <summary>
        /// Llamado por MatchesService justo después de que ambos usuarios
        /// aceptan el chat (status pasa a "ChatActivo"). Si ya existe una
        /// Conversation para ese match (por ejemplo, por un reintento),
        /// no crea una duplicada.
        /// </summary>
        Task<long> CreateFromMatchAsync(long matchId);

        /// <summary>
        /// Llamado por ContactRequestsService justo después de que el
        /// dueño del FoundItem acepta la solicitud.
        /// </summary>
        Task<long> CreateFromContactRequestAsync(long contactRequestId);

        Task<ConversationResponseDto> ConfirmReturnAsync(long conversationId);
    }
}
