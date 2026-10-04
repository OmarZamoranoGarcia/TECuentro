using TEContigo.Modules.Conversations.Services;
using TEContigo.Modules.Messages.DTOs;
using TEContigo.Modules.Messages.Models;
using TEContigo.Modules.Messages.Repositories;
using TEContigo.Shared.Pagination;
using TEContigo.Shared.Security.CurrentUser;

namespace TEContigo.Modules.Messages.Services;

public class MessagesService : IMessagesService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 50;
    private const int MaxContentLength = 2000;

    private readonly IMessagesRepository _messagesRepository;
    private readonly IConversationsService _conversationsService;
    private readonly ICurrentUserService _currentUserService;

    public MessagesService(
        IMessagesRepository messagesRepository,
        IConversationsService conversationsService,
        ICurrentUserService currentUserService)
    {
        _messagesRepository = messagesRepository;
        _conversationsService = conversationsService;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResultDto<MessageDto>> GetMessagesAsync(
        long conversationId,
        int pageNumber,
        int pageSize)
    {
        // GetByIdAsync de Conversations ya valida que exista y que el
        // usuario actual participe en ella (o sea staff) — si no,
        // lanza KeyNotFoundException/UnauthorizedAccessException por
        // su cuenta, y nunca llegamos a tocar la tabla de Messages.
        await _conversationsService.GetByIdAsync(conversationId);

        if (pageNumber < 1)
        {
            pageNumber = 1;
        }

        if (pageSize < 1)
        {
            pageSize = DefaultPageSize;
        }
        else if (pageSize > MaxPageSize)
        {
            pageSize = MaxPageSize;
        }

        var pagedMessages = await _messagesRepository.GetByConversationIdAsync(
            conversationId, pageNumber, pageSize);

        // Ver la primera página (los mensajes más recientes) implica
        // que el usuario ya los está leyendo — se marcan como leídos
        // automáticamente, sin que el frontend tenga que llamar a un
        // endpoint aparte para esto.
        if (pageNumber == 1)
        {
            await _messagesRepository.MarkConversationAsReadAsync(
                conversationId, _currentUserService.UserId);
        }

        var items = pagedMessages.Items.Select(MapToDto);

        return new PagedResultDto<MessageDto>
        {
            Items = items,
            PageNumber = pagedMessages.PageNumber,
            PageSize = pagedMessages.PageSize,
            TotalCount = pagedMessages.TotalCount
        };
    }

    public async Task<MessageResponseDto> SendMessageAsync(
        long conversationId,
        CreateMessageDto dto)
    {
        var conversation = await _conversationsService.GetByIdAsync(conversationId);

        if (conversation!.ClosedAt != null)
        {
            throw new InvalidOperationException(
                "No puedes enviar mensajes en una conversación cerrada.");
        }

        var content = dto.Content?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException(
                "El mensaje no puede estar vacío.");
        }

        if (content.Length > MaxContentLength)
        {
            throw new ArgumentException(
                $"El mensaje no puede superar los {MaxContentLength} caracteres.");
        }

        var message = new MessagesModel
        {
            ConversationId = conversationId,
            SenderId = _currentUserService.UserId,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        var id = await _messagesRepository.CreateAsync(message);

        return new MessageResponseDto
        {
            Success = true,
            Message = "Mensaje enviado.",
            Id = id
        };
    }

    public async Task MarkAsReadAsync(long conversationId)
    {
        await _conversationsService.GetByIdAsync(conversationId);

        await _messagesRepository.MarkConversationAsReadAsync(
            conversationId, _currentUserService.UserId);
    }

    private static MessageDto MapToDto(MessagesModel message)
    {
        return new MessageDto
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            Content = message.Content,
            CreatedAt = message.CreatedAt,
            ReadAt = message.ReadAt
        };
    }
}