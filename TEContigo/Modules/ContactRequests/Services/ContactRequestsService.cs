using TEContigo.Infrastructure.ImagesStorage;
using TEContigo.Modules.ContactRequests.DTOs;
using TEContigo.Modules.ContactRequests.Models;
using TEContigo.Modules.ContactRequests.Repositories;
using TEContigo.Modules.Conversations.Services;
using TEContigo.Modules.FoundItems.DTOs;
using TEContigo.Modules.FoundItems.Models;
using TEContigo.Modules.FoundItems.Repositories;
using TEContigo.Shared.Security.CurrentUser;

namespace TEContigo.Modules.ContactRequests.Services;

public class ContactRequestsService : IContactRequestsService
{
    private const string StatusPending = "Pendiente";
    private const string StatusAccepted = "Aceptada";
    private const string StatusRejected = "Rechazada";
    private const string StatusCancelled = "Cancelada";

    private const string FoundItemStatusActive = "Activo";

    private const string RoleAdmin = "ADMIN";
    private const string RoleModerator = "MODERATOR";

    private readonly IContactRequestsRepository _contactRequestsRepository;
    private readonly IFoundItemsRepository _foundItemsRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IImageService _imageService;
    private readonly IConversationsService _conversationsService;

    public ContactRequestsService(
        IContactRequestsRepository contactRequestsRepository,
        IFoundItemsRepository foundItemsRepository,
        ICurrentUserService currentUserService,
        IImageService imageService,
        IConversationsService conversationsService)
    {
        _contactRequestsRepository = contactRequestsRepository;
        _foundItemsRepository = foundItemsRepository;
        _currentUserService = currentUserService;
        _imageService = imageService;
        _conversationsService = conversationsService;
    }

    public async Task<IEnumerable<ContactRequestDto>> GetAllForCurrentUserAsync()
    {
        var isStaff =
            _currentUserService.Role == RoleAdmin ||
            _currentUserService.Role == RoleModerator;

        var contactRequests = isStaff
            ? await _contactRequestsRepository.GetAllAsync()
            : await _contactRequestsRepository.GetAllForUserAsync(_currentUserService.UserId);

        var tasks = contactRequests.Select(MapToDtoAsync);

        return await Task.WhenAll(tasks);
    }

    public async Task<ContactRequestDto?> GetByIdAsync(long id)
    {
        var contactRequest = await _contactRequestsRepository.GetByIdAsync(id);

        if (contactRequest == null)
        {
            throw new KeyNotFoundException(
                "La solicitud de contacto no existe.");
        }

        var foundItem = await GetFoundItemAsync(contactRequest.FoundItemId);

        EnsureUserIsInvolvedOrStaff(contactRequest, foundItem);

        return await BuildDtoAsync(contactRequest, foundItem);
    }

    public async Task<ContactRequestResponseDto> CreateAsync(CreateContactRequestDto dto)
    {
        var foundItem = await _foundItemsRepository.GetByIdAsync(dto.FoundItemId);

        if (foundItem == null)
        {
            throw new KeyNotFoundException(
                "La publicación de objeto encontrado no existe.");
        }

        if (foundItem.Status != FoundItemStatusActive)
        {
            throw new InvalidOperationException(
                "No puedes contactar sobre una publicación que ya no está activa.");
        }

        var requesterId = _currentUserService.UserId;

        if (foundItem.UserId == requesterId)
        {
            throw new InvalidOperationException(
                "No puedes solicitar contacto sobre tu propia publicación.");
        }

        if (await _contactRequestsRepository.ExistsPendingAsync(dto.FoundItemId, requesterId))
        {
            throw new InvalidOperationException(
                "Ya tienes una solicitud pendiente para esta publicación.");
        }

        var contactRequest = new ContactRequestsModel
        {
            FoundItemId = dto.FoundItemId,
            RequesterId = requesterId,
            MatchId = dto.MatchId,
            Status = StatusPending,
            CreatedAt = DateTime.UtcNow
        };

        var id = await _contactRequestsRepository.CreateAsync(contactRequest);

        return new ContactRequestResponseDto
        {
            Success = true,
            Message = "Solicitud de contacto enviada correctamente.",
            Id = id
        };
    }

    public async Task<ContactRequestResponseDto> AcceptAsync(long id)
    {
        var contactRequest = await _contactRequestsRepository.GetByIdAsync(id);

        if (contactRequest == null)
        {
            throw new KeyNotFoundException(
                "La solicitud de contacto no existe.");
        }

        var foundItem = await GetFoundItemAsync(contactRequest.FoundItemId);

        // Solo el dueño de la publicación de objeto encontrado puede
        // aceptar — es quien decide si le abre la puerta a quien toca.
        if (foundItem.UserId != _currentUserService.UserId)
        {
            throw new UnauthorizedAccessException(
                "Solo el dueño de la publicación puede aceptar esta solicitud.");
        }

        var applied = await _contactRequestsRepository.TryRespondAsync(
            id, StatusPending, StatusAccepted, DateTime.UtcNow);

        if (!applied)
        {
            throw new InvalidOperationException(
                $"Esta solicitud ya fue respondida (status actual: '{contactRequest.Status}').");
        }

        // Aquí es donde, cuando construyas ConversationsService, debe
        // engancharse la creación de la Conversation con
        // contact_request_id = id (simétrico al hook que dejamos en
        // MatchesService.RequestChatAsync para el otro camino).
        await _conversationsService.CreateFromContactRequestAsync(id);

        return new ContactRequestResponseDto
        {
            Success = true,
            Message = "Solicitud aceptada. Ya pueden comenzar a conversar.",
            Id = id
        };
    }

    public async Task<ContactRequestResponseDto> RejectAsync(long id)
    {
        var contactRequest = await _contactRequestsRepository.GetByIdAsync(id);

        if (contactRequest == null)
        {
            throw new KeyNotFoundException(
                "La solicitud de contacto no existe.");
        }

        var foundItem = await GetFoundItemAsync(contactRequest.FoundItemId);

        if (foundItem.UserId != _currentUserService.UserId)
        {
            throw new UnauthorizedAccessException(
                "Solo el dueño de la publicación puede rechazar esta solicitud.");
        }

        var applied = await _contactRequestsRepository.TryRespondAsync(
            id, StatusPending, StatusRejected, DateTime.UtcNow);

        if (!applied)
        {
            throw new InvalidOperationException(
                $"Esta solicitud ya fue respondida (status actual: '{contactRequest.Status}').");
        }

        return new ContactRequestResponseDto
        {
            Success = true,
            Message = "Solicitud rechazada.",
            Id = id
        };
    }

    public async Task<ContactRequestResponseDto> CancelAsync(long id)
    {
        var contactRequest = await _contactRequestsRepository.GetByIdAsync(id);

        if (contactRequest == null)
        {
            throw new KeyNotFoundException(
                "La solicitud de contacto no existe.");
        }

        // Cancelar es lo opuesto a aceptar/rechazar: solo quien la
        // envió puede arrepentirse y cancelarla.
        if (contactRequest.RequesterId != _currentUserService.UserId)
        {
            throw new UnauthorizedAccessException(
                "Solo quien envió la solicitud puede cancelarla.");
        }

        var applied = await _contactRequestsRepository.TryRespondAsync(
            id, StatusPending, StatusCancelled, DateTime.UtcNow);

        if (!applied)
        {
            throw new InvalidOperationException(
                $"Esta solicitud ya fue respondida (status actual: '{contactRequest.Status}').");
        }

        return new ContactRequestResponseDto
        {
            Success = true,
            Message = "Solicitud cancelada.",
            Id = id
        };
    }

    private async Task<FoundItemsModel> GetFoundItemAsync(long foundItemId)
    {
        var foundItem = await _foundItemsRepository.GetByIdAsync(foundItemId);

        if (foundItem == null)
        {
            throw new InvalidOperationException(
                "La solicitud hace referencia a una publicación que ya no existe.");
        }

        return foundItem;
    }

    private void EnsureUserIsInvolvedOrStaff(
        ContactRequestsModel contactRequest,
        FoundItemsModel foundItem)
    {
        var currentUserId = _currentUserService.UserId;

        var isInvolved =
            contactRequest.RequesterId == currentUserId ||
            foundItem.UserId == currentUserId;

        var isStaff =
            _currentUserService.Role == RoleAdmin ||
            _currentUserService.Role == RoleModerator;

        if (!isInvolved && !isStaff)
        {
            throw new UnauthorizedAccessException(
                "No tienes permiso para ver esta solicitud.");
        }
    }

    private async Task<ContactRequestDto> MapToDtoAsync(ContactRequestsModel contactRequest)
    {
        var foundItem = await GetFoundItemAsync(contactRequest.FoundItemId);

        return await BuildDtoAsync(contactRequest, foundItem);
    }

    private async Task<ContactRequestDto> BuildDtoAsync(
        ContactRequestsModel contactRequest,
        FoundItemsModel foundItem)
    {
        var photoUrl = await GetPhotoUrlAsync(foundItem.PhotoPath);

        return new ContactRequestDto
        {
            Id = contactRequest.Id,
            RequesterId = contactRequest.RequesterId,
            MatchId = contactRequest.MatchId,
            Status = contactRequest.Status,
            CreatedAt = contactRequest.CreatedAt,
            RespondedAt = contactRequest.RespondedAt,
            FoundItem = new FoundItemDto
            {
                Id = foundItem.Id,
                UserId = foundItem.UserId,
                Category = foundItem.Category,
                Article = foundItem.Article,
                Color = foundItem.Color,
                Location = foundItem.Location,
                Description = foundItem.Description,
                PhotoUrl = photoUrl,
                Status = foundItem.Status,
                CreatedAt = foundItem.CreatedAt,
                UpdatedAt = foundItem.UpdatedAt
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
}