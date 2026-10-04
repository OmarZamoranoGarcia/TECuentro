using TEContigo.Modules.Conversations.Models;

namespace TEContigo.Modules.Conversations.Repositories
{
    public interface IConversationsRepository
    {
        /// <summary>
        /// Sin filtrar. Pensado para ADMIN/MODERATOR.
        /// </summary>
        Task<IEnumerable<ConversationsModel>> GetAllAsync();

        /// <summary>
        /// Conversaciones donde el usuario participa, sin importar si
        /// vienen de un Match o de un ContactRequest (requiere JOIN a
        /// ambos caminos posibles).
        /// </summary>
        Task<IEnumerable<ConversationsModel>> GetAllForUserAsync(long userId);

        Task<ConversationsModel?> GetByIdAsync(long id);

        Task<ConversationsModel?> GetByMatchIdAsync(long matchId);

        Task<ConversationsModel?> GetByContactRequestIdAsync(long contactRequestId);

        Task<long> CreateFromMatchAsync(long matchId);

        Task<long> CreateFromContactRequestAsync(long contactRequestId);

        /// <summary>
        /// Update atómico condicionado: solo marca la confirmación en TRUE
        /// si actualmente está en FALSE. Mismo patrón que ya usamos en
        /// Matches, ahora con nombres genéricos (requester/finder) para
        /// que sirva sin importar el origen de la conversación.
        /// </summary>
        Task<(bool RequesterConfirmed, bool FinderConfirmed)?> TryConfirmReturnAsync(
            long conversationId,
            bool isRequester);

        /// <summary>
        /// Cierra la conversación de forma atómica: solo aplica si
        /// closed_at todavía es NULL (evita cerrarla dos veces).
        /// </summary>
        Task<bool> TryCloseAsync(long conversationId);
    }
}
