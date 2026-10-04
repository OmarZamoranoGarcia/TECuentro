using TEContigo.Infrastructure.ImagesStorage;
using TEContigo.Modules.ContactRequests.Models;
using TEContigo.Modules.ContactRequests.Repositories;
using TEContigo.Modules.Conversations.DTOs;
using TEContigo.Modules.Conversations.Models;
using TEContigo.Modules.Conversations.Repositories;
using TEContigo.Modules.FoundItems.DTOs;
using TEContigo.Modules.FoundItems.Models;
using TEContigo.Modules.FoundItems.Repositories;
using TEContigo.Modules.LostItems.DTOs;
using TEContigo.Modules.LostItems.Models;
using TEContigo.Modules.LostItems.Repositories;
using TEContigo.Modules.Matches.Models;
using TEContigo.Modules.Matches.Repositories;
using TEContigo.Shared.Security.CurrentUser;

namespace TEContigo.Modules.Conversations.Services;

public class ConversationsService : IConversationsService
{
    private const string RoleAdmin = "ADMIN";
    private const string RoleModerator = "MODERATOR";

    private const string ItemStatusReturned = "Devuelto";

    private const string MatchStatusChatActive = "ChatActivo";
    private const string MatchStatusClosed = "Cerrado";

    private readonly IConversationsRepository _conversationsRepository;
    private readonly IMatchesRepository _matchesRepository;
    private readonly IContactRequestsRepository _contactRequestsRepository;
    private readonly ILostItemsRepository _lostItemsRepository;
    private readonly IFoundItemsRepository _foundItemsRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IImageService _imageService;

    public ConversationsService(
        IConversationsRepository conversationsRepository,
        IMatchesRepository matchesRepository,
        IContactRequestsRepository contactRequestsRepository,
        ILostItemsRepository lostItemsRepository,
        IFoundItemsRepository foundItemsRepository,
        ICurrentUserService currentUserService,
        IImageService imageService)
    {
        _conversationsRepository = conversationsRepository;
        _matchesRepository = matchesRepository;
        _contactRequestsRepository = contactRequestsRepository;
        _lostItemsRepository = lostItemsRepository;
        _foundItemsRepository = foundItemsRepository;
        _currentUserService = currentUserService;
        _imageService = imageService;
    }

    public async Task<IEnumerable<ConversationDto>> GetAllForCurrentUserAsync()
    {
        var isStaff =
            _currentUserService.Role == RoleAdmin ||
            _currentUserService.Role == RoleModerator;

        var conversations = isStaff
            ? await _conversationsRepository.GetAllAsync()
            : await _conversationsRepository.GetAllForUserAsync(_currentUserService.UserId);

        var tasks = conversations.Select(MapToDtoAsync);

        return await Task.WhenAll(tasks);
    }

    public async Task<ConversationDto?> GetByIdAsync(long id)
    {
        var conversation = await _conversationsRepository.GetByIdAsync(id);

        if (conversation == null)
        {
            throw new KeyNotFoundException(
                "La conversación no existe.");
        }

        var participants = await GetParticipantsAsync(conversation);

        EnsureUserIsInvolvedOrStaff(participants);

        return await BuildDtoAsync(conversation, participants);
    }

    public async Task<long> CreateFromMatchAsync(long matchId)
    {
        // Idempotencia: si por cualquier motivo ya existe una
        // Conversation para este match (reintento, doble llamada),
        // no creamos una duplicada.
        var existing = await _conversationsRepository.GetByMatchIdAsync(matchId);

        if (existing != null)
        {
            return existing.Id;
        }

        return await _conversationsRepository.CreateFromMatchAsync(matchId);
    }

    public async Task<long> CreateFromContactRequestAsync(long contactRequestId)
    {
        var existing = await _conversationsRepository.GetByContactRequestIdAsync(contactRequestId);

        if (existing != null)
        {
            return existing.Id;
        }

        return await _conversationsRepository.CreateFromContactRequestAsync(contactRequestId);
    }

    public async Task<ConversationResponseDto> ConfirmReturnAsync(long conversationId)
    {
        var conversation = await _conversationsRepository.GetByIdAsync(conversationId);

        if (conversation == null)
        {
            throw new KeyNotFoundException(
                "La conversación no existe.");
        }

        if (conversation.ClosedAt != null)
        {
            throw new InvalidOperationException(
                "Esta conversación ya está cerrada.");
        }

        var participants = await GetParticipantsAsync(conversation);

        var currentUserId = _currentUserService.UserId;

        var isRequester = participants.RequesterId == currentUserId;
        var isFinder = participants.FinderId == currentUserId;

        if (!isRequester && !isFinder)
        {
            throw new UnauthorizedAccessException(
                "No tienes permiso para confirmar la devolución en esta conversación.");
        }

        // No es un switch, igual que en Matches: una vez confirmada no
        // se puede deshacer.
        var alreadyConfirmed = isRequester
            ? conversation.RequesterConfirmedReturn
            : conversation.FinderConfirmedReturn;

        if (alreadyConfirmed)
        {
            throw new InvalidOperationException(
                "Ya habías confirmado la devolución de este objeto.");
        }

        var result = await _conversationsRepository.TryConfirmReturnAsync(
            conversationId, isRequester);

        if (result == null)
        {
            throw new KeyNotFoundException(
                "La conversación no existe.");
        }

        var bothConfirmed = result.Value.RequesterConfirmed && result.Value.FinderConfirmed;

        if (bothConfirmed)
        {
            await _conversationsRepository.TryCloseAsync(conversationId);

            // El FoundItem siempre existe, sin importar el origen.
            participants.FoundItem.Status = ItemStatusReturned;
            participants.FoundItem.UpdatedAt = DateTime.UtcNow;
            await _foundItemsRepository.UpdateAsync(participants.FoundItem);

            // El LostItem y el Match solo existen si el origen fue un
            // Match — en el camino de ContactRequest no hay nada más
            // que cerrar.
            if (participants.LostItem != null && conversation.MatchId != null)
            {
                participants.LostItem.Status = ItemStatusReturned;
                participants.LostItem.UpdatedAt = DateTime.UtcNow;
                await _lostItemsRepository.UpdateAsync(participants.LostItem);

                await _matchesRepository.TryTransitionStatusAsync(
                    conversation.MatchId.Value,
                    MatchStatusChatActive,
                    MatchStatusClosed);
            }
        }

        return new ConversationResponseDto
        {
            Success = true,
            Message = bothConfirmed
                ? "Ambos confirmaron la devolución. La conversación se cerró."
                : "Confirmación registrada, esperando a la otra persona.",
            Id = conversationId,
            BothConfirmed = bothConfirmed
        };
    }

    /// <summary>
    /// Resuelve quién es el "requester" (quien busca algo) y quién es
    /// el "finder" (quien publicó el FoundItem), sin importar si la
    /// conversación nació de un Match o de un ContactRequest.
    /// </summary>
    private async Task<ConversationParticipants> GetParticipantsAsync(
        ConversationsModel conversation)
    {
        if (conversation.MatchId != null)
        {
            var match = await _matchesRepository.GetByIdAsync(conversation.MatchId.Value);

            if (match == null)
            {
                throw new InvalidOperationException(
                    "La conversación hace referencia a un match que ya no existe.");
            }

            var lostItem = await _lostItemsRepository.GetByIdAsync(match.LostItemId);
            var foundItem = await _foundItemsRepository.GetByIdAsync(match.FoundItemId);

            if (lostItem == null || foundItem == null)
            {
                throw new InvalidOperationException(
                    "La conversación hace referencia a publicaciones que ya no existen.");
            }

            return new ConversationParticipants
            {
                RequesterId = lostItem.UserId,
                FinderId = foundItem.UserId,
                LostItem = lostItem,
                FoundItem = foundItem
            };
        }

        if (conversation.ContactRequestId != null)
        {
            var contactRequest = await _contactRequestsRepository.GetByIdAsync(
                conversation.ContactRequestId.Value);

            if (contactRequest == null)
            {
                throw new InvalidOperationException(
                    "La conversación hace referencia a una solicitud de contacto que ya no existe.");
            }

            var foundItem = await _foundItemsRepository.GetByIdAsync(contactRequest.FoundItemId);

            if (foundItem == null)
            {
                throw new InvalidOperationException(
                    "La conversación hace referencia a una publicación que ya no existe.");
            }

            return new ConversationParticipants
            {
                RequesterId = contactRequest.RequesterId,
                FinderId = foundItem.UserId,
                LostItem = null,
                FoundItem = foundItem
            };
        }

        // No debería pasar nunca — el CHECK constraint de la tabla
        // garantiza que uno de los dos siempre está presente.
        throw new InvalidOperationException(
            "La conversación no tiene un origen válido (ni match ni contact request).");
    }

    private void EnsureUserIsInvolvedOrStaff(ConversationParticipants participants)
    {
        var currentUserId = _currentUserService.UserId;

        var isInvolved =
            participants.RequesterId == currentUserId ||
            participants.FinderId == currentUserId;

        var isStaff =
            _currentUserService.Role == RoleAdmin ||
            _currentUserService.Role == RoleModerator;

        if (!isInvolved && !isStaff)
        {
            throw new UnauthorizedAccessException(
                "No tienes permiso para ver esta conversación.");
        }
    }

    private async Task<ConversationDto> MapToDtoAsync(ConversationsModel conversation)
    {
        var participants = await GetParticipantsAsync(conversation);

        return await BuildDtoAsync(conversation, participants);
    }

    private async Task<ConversationDto> BuildDtoAsync(
        ConversationsModel conversation,
        ConversationParticipants participants)
    {
        var foundPhotoUrl = await GetPhotoUrlAsync(participants.FoundItem.PhotoPath);

        LostItemDto? lostItemDto = null;

        if (participants.LostItem != null)
        {
            var lostPhotoUrl = await GetPhotoUrlAsync(participants.LostItem.PhotoPath);

            lostItemDto = new LostItemDto
            {
                Id = participants.LostItem.Id,
                UserId = participants.LostItem.UserId,
                Category = participants.LostItem.Category,
                Article = participants.LostItem.Article,
                Color = participants.LostItem.Color,
                Location = participants.LostItem.Location,
                Description = participants.LostItem.Description,
                PhotoUrl = lostPhotoUrl,
                Status = participants.LostItem.Status,
                CreatedAt = participants.LostItem.CreatedAt,
                UpdatedAt = participants.LostItem.UpdatedAt
            };
        }

        return new ConversationDto
        {
            Id = conversation.Id,
            MatchId = conversation.MatchId,
            ContactRequestId = conversation.ContactRequestId,
            RequesterId = participants.RequesterId,
            FinderId = participants.FinderId,
            RequesterConfirmedReturn = conversation.RequesterConfirmedReturn,
            FinderConfirmedReturn = conversation.FinderConfirmedReturn,
            CreatedAt = conversation.CreatedAt,
            ClosedAt = conversation.ClosedAt,
            LostItem = lostItemDto,
            FoundItem = new FoundItemDto
            {
                Id = participants.FoundItem.Id,
                UserId = participants.FoundItem.UserId,
                Category = participants.FoundItem.Category,
                Article = participants.FoundItem.Article,
                Color = participants.FoundItem.Color,
                Location = participants.FoundItem.Location,
                Description = participants.FoundItem.Description,
                PhotoUrl = foundPhotoUrl,
                Status = participants.FoundItem.Status,
                CreatedAt = participants.FoundItem.CreatedAt,
                UpdatedAt = participants.FoundItem.UpdatedAt
            }
        };
    }

    private async Task<string?> GetPhotoUrlAsync(string? photoPath)
    {
        if (string.IsNullOrWhiteSpace(photoPath))
        {
            return null;
        }

        return await _imageService.GetUrlAsync(photoPath);
    }

    private sealed class ConversationParticipants
    {
        public long RequesterId { get; set; }
        public long FinderId { get; set; }
        public LostItemsModel? LostItem { get; set; }
        public FoundItemsModel FoundItem { get; set; } = null!;
    }
}