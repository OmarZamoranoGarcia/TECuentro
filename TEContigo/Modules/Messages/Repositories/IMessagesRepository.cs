using TEContigo.Modules.Messages.Models;
using TEContigo.Shared.Pagination;

namespace TEContigo.Modules.Messages.Repositories
{
    public interface IMessagesRepository
    {
        /// <summary>
        /// Mensajes de una conversación, más recientes primero (igual que
        /// el resto del proyecto); el frontend decide si los invierte para
        /// mostrarlos en orden cronológico ascendente al renderizar.
        /// </summary>
        Task<PagedResultDto<MessagesModel>> GetByConversationIdAsync(
            long conversationId,
            int pageNumber,
            int pageSize);

        Task<long> CreateAsync(MessagesModel message);

        /// <summary>
        /// Marca como leídos todos los mensajes de la conversación que
        /// NO fueron enviados por readerUserId (no tiene sentido marcar
        /// tus propios mensajes como "leídos por ti mismo").
        /// </summary>
        Task MarkConversationAsReadAsync(long conversationId, long readerUserId);
    }
}
