using Npgsql;
using TEContigo.Infrastructure.ImagesStorage;
using TEContigo.Modules.FoundItems.DTOs;
using TEContigo.Modules.FoundItems.Models;
using TEContigo.Modules.FoundItems.Repositories;
using TEContigo.Modules.LostItems.DTOs;
using TEContigo.Modules.LostItems.Models;
using TEContigo.Modules.LostItems.Repositories;
using TEContigo.Modules.Matches.DTOs;
using TEContigo.Modules.Matches.Models;
using TEContigo.Modules.Matches.Repositories;
using TEContigo.Shared.Matching;
using TEContigo.Shared.Pagination;
using TEContigo.Shared.Security.CurrentUser;

namespace TEContigo.Modules.Matches.Services;

public class MatchesService : IMatchesService
{
    // Pesos del algoritmo de matching (deben sumar 100).
    // Category actúa como filtro: si no coincide, se descarta el par
    // sin calcular nada más (ahorra trabajo y evita falsos positivos
    // entre categorías distintas, ej. "Mochila" vs "Celular").
    private const decimal CategoryWeight = 30m;
    private const decimal ArticleWeight = 20m;
    private const decimal ColorWeight = 15m;
    private const decimal LocationWeight = 15m;
    private const decimal DescriptionWeight = 20m;

    // Umbral mínimo para que se cree el Match.
    // qué tan estricto quieras que sea el sistema.
    private const decimal MatchThreshold = 40m;

    private const string StatusActive = "Activo";
    private const string RoleAdmin = "ADMIN";
    private const string RoleModerator = "MODERATOR";

    // Máquina de estados del Match.
    private const string MatchStatusPending = "Pendiente";
    private const string MatchStatusChatActive = "ChatActivo";
    private const string MatchStatusClosed = "Cerrado";

    // Status que reciben LostItem/FoundItem cuando el Match se cierra.
    private const string ItemStatusReturned = "Devuelto";

    // Código de error de PostgreSQL para violación de constraint UNIQUE.
    private const string PostgresUniqueViolation = "23505";

    // Pagination
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 50;

    private readonly IMatchesRepository _matchesRepository;
    private readonly ILostItemsRepository _lostItemsRepository;
    private readonly IFoundItemsRepository _foundItemsRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IImageService _imageService;

    public MatchesService(
        IMatchesRepository matchesRepository,
        ILostItemsRepository lostItemsRepository,
        IFoundItemsRepository foundItemsRepository,
        ICurrentUserService currentUserService,
        IImageService imageService)
    {
        _matchesRepository = matchesRepository;
        _lostItemsRepository = lostItemsRepository;
        _foundItemsRepository = foundItemsRepository;
        _currentUserService = currentUserService;
        _imageService = imageService;
    }

    public async Task<PagedResultDto<MatchDto>> GetAllForCurrentUserAsync(
    int pageNumber,
    int pageSize)
    {
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

        var isStaff =
            _currentUserService.Role == RoleAdmin ||
            _currentUserService.Role == RoleModerator;

        var pagedMatches = isStaff
            ? await _matchesRepository.GetAllAsync(pageNumber, pageSize)
            : await _matchesRepository.GetAllForUserAsync(
                _currentUserService.UserId, pageNumber, pageSize);

        var tasks = pagedMatches.Items.Select(MapToDtoAsync);

        var items = await Task.WhenAll(tasks);

        return new PagedResultDto<MatchDto>
        {
            Items = items,
            PageNumber = pagedMatches.PageNumber,
            PageSize = pagedMatches.PageSize,
            TotalCount = pagedMatches.TotalCount
        };
    }

    public async Task<MatchDto?> GetByIdAsync(long id)
    {
        var match = await _matchesRepository.GetByIdAsync(id);

        if (match == null)
        {
            throw new KeyNotFoundException(
                "El match no existe.");
        }

        var (lostItem, foundItem) = await GetItemsAsync(match);

        EnsureUserIsInvolvedOrStaff(lostItem, foundItem);

        return await BuildDtoAsync(match, lostItem, foundItem);
    }

    public async Task GenerateMatchesForLostItemAsync(long lostItemId)
    {
        var lostItem = await _lostItemsRepository.GetByIdAsync(lostItemId);

        if (lostItem == null)
        {
            return;
        }

        var activeFoundItems = await _foundItemsRepository.GetActiveAsync();

        foreach (var foundItem in activeFoundItems)
        {
            await TryCreateMatchAsync(lostItem, foundItem);
        }
    }

    public async Task GenerateMatchesForFoundItemAsync(long foundItemId)
    {
        var foundItem = await _foundItemsRepository.GetByIdAsync(foundItemId);

        if (foundItem == null)
        {
            return;
        }

        var activeLostItems = await _lostItemsRepository.GetActiveAsync();

        foreach (var lostItem in activeLostItems)
        {
            await TryCreateMatchAsync(lostItem, foundItem);
        }
    }

    public async Task<MatchResponseDto> RequestChatAsync(long matchId)
    {
        var match = await _matchesRepository.GetByIdAsync(matchId);

        if (match == null)
        {
            throw new KeyNotFoundException(
                "El match no existe.");
        }

        var (lostItem, foundItem) = await GetItemsAsync(match);

        var currentUserId = _currentUserService.UserId;

        var isLostUser = lostItem.UserId == currentUserId;
        var isFoundUser = foundItem.UserId == currentUserId;

        if (!isLostUser && !isFoundUser)
        {
            throw new UnauthorizedAccessException(
                "No tienes permiso para solicitar chat en este match.");
        }

        // Solo se puede activar/desactivar la solicitud mientras el
        // match sigue en "Pendiente". Una vez que ambos aceptaron y
        // pasó a "ChatActivo", queda bloqueado (ya no es un switch).
        if (match.Status != MatchStatusPending)
        {
            throw new InvalidOperationException(
                $"No puedes modificar la solicitud de chat: el match ya está en estado '{match.Status}'.");
        }

        // Si por alguna razón el mismo usuario es dueño de ambos
        // objetos (encontró su propia publicación de pérdida), se
        // trata como el rol de "lost user" por defecto.
        var (lostRequested, foundRequested) = await _matchesRepository.ToggleChatRequestAsync(
            matchId,
            isLostUser);

        var myNewValue = isLostUser ? lostRequested : foundRequested;
        var bothAccepted = lostRequested && foundRequested;

        if (bothAccepted)
        {
            // Transición atómica condicionada: si dos peticiones llegaran
            // a completar el toggle casi al mismo tiempo, solo una va a
            // encontrar el status todavía en "Pendiente" y aplicar el
            // cambio; la otra no hace nada de más (evita doble transición).
            await _matchesRepository.TryTransitionStatusAsync(
                matchId,
                MatchStatusPending,
                MatchStatusChatActive);
        }

        return new MatchResponseDto
        {
            Success = true,
            Message = bothAccepted
                ? "Ambos usuarios aceptaron el chat. Ya pueden comenzar a conversar."
                : myNewValue
                    ? "Has solicitado el chat. Esperando a que el otro usuario acepte."
                    : "Has cancelado tu solicitud de chat.",
            Id = matchId,
            BothUsersAccepted = bothAccepted
        };
    }

    public async Task<MatchResponseDto> ConfirmReturnAsync(long matchId)
    {
        var match = await _matchesRepository.GetByIdAsync(matchId);

        if (match == null)
        {
            throw new KeyNotFoundException(
                "El match no existe.");
        }

        var (lostItem, foundItem) = await GetItemsAsync(match);

        var currentUserId = _currentUserService.UserId;

        var isLostUser = lostItem.UserId == currentUserId;
        var isFoundUser = foundItem.UserId == currentUserId;

        if (!isLostUser && !isFoundUser)
        {
            throw new UnauthorizedAccessException(
                "No tienes permiso para confirmar la devolución en este match.");
        }

        // Regla de negocio: no se puede confirmar devolución si el chat
        // todavía no está activo, ni si el match ya se cerró.
        if (match.Status != MatchStatusChatActive)
        {
            throw new InvalidOperationException(
                match.Status == MatchStatusPending
                    ? "No puedes confirmar la devolución hasta que ambos usuarios acepten el chat."
                    : $"Este match ya está en estado '{match.Status}' y no se puede modificar.");
        }

        // A diferencia del chat, esta confirmación NO es un switch: una
        // vez confirmada no se puede deshacer, así que si el usuario ya
        // había confirmado, se lo indicamos en vez de fingir que se
        // "confirmó de nuevo" sin más.
        var alreadyConfirmed = isLostUser
            ? match.LostUserReturnConfirmed
            : match.FoundUserReturnConfirmed;

        if (alreadyConfirmed)
        {
            throw new InvalidOperationException(
                "Ya habías confirmado la devolución de este objeto.");
        }

        var result = await _matchesRepository.TryConfirmReturnAsync(
            matchId,
            isLostUser);

        if (result == null)
        {
            throw new KeyNotFoundException(
                "El match no existe.");
        }

        var bothConfirmed = result.Value.LostConfirmed && result.Value.FoundConfirmed;

        if (bothConfirmed)
        {
            await _matchesRepository.TryTransitionStatusAsync(
                matchId,
                MatchStatusChatActive,
                MatchStatusClosed);

            lostItem.Status = ItemStatusReturned;
            lostItem.UpdatedAt = DateTime.UtcNow;
            await _lostItemsRepository.UpdateAsync(lostItem);

            foundItem.Status = ItemStatusReturned;
            foundItem.UpdatedAt = DateTime.UtcNow;
            await _foundItemsRepository.UpdateAsync(foundItem);
        }

        return new MatchResponseDto
        {
            Success = true,
            Message = bothConfirmed
                ? "Ambos usuarios confirmaron la devolución. El match se cerró."
                : "Confirmación registrada, esperando al otro usuario.",
            Id = matchId,
            BothUsersAccepted = bothConfirmed
        };
    }

    private async Task TryCreateMatchAsync(
        LostItemsModel lostItem,
        FoundItemsModel foundItem)
    {
        if (!string.Equals(
                lostItem.Category,
                foundItem.Category,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (await _matchesRepository.ExistsAsync(lostItem.Id, foundItem.Id))
        {
            return;
        }

        var articleSimilarity = TextSimilarity.CalculateSimilarity(
            lostItem.Article, foundItem.Article);

        var colorSimilarity = TextSimilarity.CalculateSimilarity(
            lostItem.Color, foundItem.Color);

        var locationSimilarity = TextSimilarity.CalculateSimilarity(
            lostItem.Location, foundItem.Location);

        var descriptionSimilarity = TextSimilarity.CalculateSimilarity(
            lostItem.Description, foundItem.Description);

        var percentage =
            CategoryWeight +
            ArticleWeight * (decimal)(articleSimilarity / 100.0) +
            ColorWeight * (decimal)(colorSimilarity / 100.0) +
            LocationWeight * (decimal)(locationSimilarity / 100.0) +
            DescriptionWeight * (decimal)(descriptionSimilarity / 100.0);

        percentage = Math.Round(percentage, 2);

        if (percentage < MatchThreshold)
        {
            return;
        }

        var match = new MatchesModel
        {
            LostItemId = lostItem.Id,
            FoundItemId = foundItem.Id,
            MatchPercentage = percentage,
            Status = MatchStatusPending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            await _matchesRepository.CreateAsync(match);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresUniqueViolation)
        {
            // Otro proceso creó el mismo par (lost_item_id, found_item_id)
            // justo antes que nosotros (por ejemplo, si GenerateMatches
            // para ambos lados corrieran casi al mismo tiempo). El
            // constraint UNIQUE de la tabla ya protegió la integridad,
            // así que simplemente ignoramos el duplicado.
        }
    }

    private async Task<(LostItemsModel LostItem, FoundItemsModel FoundItem)> GetItemsAsync(
        MatchesModel match)
    {
        var lostItem = await _lostItemsRepository.GetByIdAsync(match.LostItemId);
        var foundItem = await _foundItemsRepository.GetByIdAsync(match.FoundItemId);

        if (lostItem == null || foundItem == null)
        {
            throw new InvalidOperationException(
                "El match hace referencia a publicaciones que ya no existen.");
        }

        return (lostItem, foundItem);
    }

    private void EnsureUserIsInvolvedOrStaff(
        LostItemsModel lostItem,
        FoundItemsModel foundItem)
    {
        var currentUserId = _currentUserService.UserId;

        var isInvolved =
            lostItem.UserId == currentUserId ||
            foundItem.UserId == currentUserId;

        var isStaff =
            _currentUserService.Role == RoleAdmin ||
            _currentUserService.Role == RoleModerator;

        if (!isInvolved && !isStaff)
        {
            throw new UnauthorizedAccessException(
                "No tienes permiso para ver este match.");
        }
    }

    private async Task<MatchDto> MapToDtoAsync(MatchesModel match)
    {
        var (lostItem, foundItem) = await GetItemsAsync(match);

        return await BuildDtoAsync(match, lostItem, foundItem);
    }

    private async Task<MatchDto> BuildDtoAsync(
        MatchesModel match,
        LostItemsModel lostItem,
        FoundItemsModel foundItem)
    {
        var lostPhotoUrl = await GetPhotoUrlAsync(lostItem.PhotoPath);
        var foundPhotoUrl = await GetPhotoUrlAsync(foundItem.PhotoPath);

        return new MatchDto
        {
            Id = match.Id,
            MatchPercentage = match.MatchPercentage,
            Status = match.Status,
            LostUserChatRequest = match.LostUserChatRequest,
            FoundUserChatRequest = match.FoundUserChatRequest,
            LostUserReturnConfirmed = match.LostUserReturnConfirmed,
            FoundUserReturnConfirmed = match.FoundUserReturnConfirmed,
            CreatedAt = match.CreatedAt,
            UpdatedAt = match.UpdatedAt,
            LostItem = new LostItemDto
            {
                Id = lostItem.Id,
                UserId = lostItem.UserId,
                Category = lostItem.Category,
                Article = lostItem.Article,
                Color = lostItem.Color,
                Location = lostItem.Location,
                Description = lostItem.Description,
                PhotoUrl = lostPhotoUrl,
                Status = lostItem.Status,
                CreatedAt = lostItem.CreatedAt,
                UpdatedAt = lostItem.UpdatedAt
            },
            FoundItem = new FoundItemDto
            {
                Id = foundItem.Id,
                UserId = foundItem.UserId,
                Category = foundItem.Category,
                Article = foundItem.Article,
                Color = foundItem.Color,
                Location = foundItem.Location,
                Description = foundItem.Description,
                PhotoUrl = foundPhotoUrl,
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