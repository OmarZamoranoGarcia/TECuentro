using TEContigo.Infrastructure.Push;
using TEContigo.Modules.Notifications.DTOs;
using TEContigo.Modules.Notifications.Models;
using TEContigo.Modules.Notifications.Repositories;
using TEContigo.Shared.Pagination;
using TEContigo.Shared.Security.CurrentUser;

namespace TEContigo.Modules.Notifications.Services;

public class NotificationsService : INotificationsService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 50;

    // Notifications.title es VARCHAR(50).
    private const int MaxTitleLength = 50;

    private readonly INotificationsRepository _notificationsRepository;
    private readonly IPushQueue _pushQueue;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<NotificationsService> _logger;

    public NotificationsService(
        INotificationsRepository notificationsRepository,
        IPushQueue pushQueue,
        ICurrentUserService currentUserService,
        ILogger<NotificationsService> logger)
    {
        _notificationsRepository = notificationsRepository;
        _pushQueue = pushQueue;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<PagedResultDto<NotificationDto>> GetAllForCurrentUserAsync(
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

        var paged = await _notificationsRepository.GetByUserIdAsync(
            _currentUserService.UserId, pageNumber, pageSize);

        return new PagedResultDto<NotificationDto>
        {
            Items = paged.Items.Select(MapToDto).ToList(),
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount
        };
    }

    public Task<int> GetUnreadCountAsync()
    {
        return _notificationsRepository.CountUnreadAsync(_currentUserService.UserId);
    }

    public async Task MarkAsReadAsync(long id)
    {
        var updated = await _notificationsRepository.MarkAsReadAsync(
            id, _currentUserService.UserId);

        if (!updated)
        {
            throw new KeyNotFoundException(
                "La notificación no existe.");
        }
    }

    public async Task MarkAllAsReadAsync()
    {
        await _notificationsRepository.MarkAllAsReadAsync(_currentUserService.UserId);
    }

    public async Task NotifyAsync(
        long userId,
        string type,
        string title,
        string message,
        string referenceType,
        long referenceId,
        bool collapseIfUnread = false)
    {
        try
        {
            if (collapseIfUnread &&
                await _notificationsRepository.HasUnreadAsync(
                    userId, type, referenceType, referenceId))
            {
                return;
            }

            var notification = new NotificationsModel
            {
                UserId = userId,
                Type = type,
                Title = Truncate(title, MaxTitleLength),
                Message = message,
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            var id = await _notificationsRepository.CreateAsync(notification);

            // Lo que la app necesita para navegar al tocar el aviso.
            var data = new Dictionary<string, object>
            {
                ["notificationId"] = id,
                ["type"] = type,
                ["referenceType"] = referenceType,
                ["referenceId"] = referenceId
            };

            _pushQueue.Enqueue(new PushRequest(userId, notification.Title, message, data));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "No se pudo crear la notificación {Type} para el usuario {UserId}.",
                type,
                userId);
        }
    }

    public async Task MarkReferenceAsReadAsync(
        long userId,
        string referenceType,
        long referenceId)
    {
        try
        {
            await _notificationsRepository.MarkReferenceAsReadAsync(
                userId, referenceType, referenceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "No se pudieron marcar como leídas las notificaciones de {ReferenceType} {ReferenceId}.",
                referenceType,
                referenceId);
        }
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength
            ? value
            : value[..(maxLength - 1)] + "…";
    }

    private static NotificationDto MapToDto(NotificationsModel notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            Type = notification.Type,
            Title = notification.Title,
            Message = notification.Message,
            ReferenceType = notification.ReferenceType,
            ReferenceId = notification.ReferenceId,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt
        };
    }
}