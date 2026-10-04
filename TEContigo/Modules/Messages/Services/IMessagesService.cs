using TEContigo.Modules.Messages.DTOs;
using TEContigo.Shared.Pagination;

namespace TEContigo.Modules.Messages.Services
{
    public interface IMessagesService
    {
        Task<PagedResultDto<MessageDto>> GetMessagesAsync(
        long conversationId,
        int pageNumber,
        int pageSize);

        Task<MessageResponseDto> SendMessageAsync(
            long conversationId,
            CreateMessageDto dto);

        Task MarkAsReadAsync(long conversationId);
    }
}
