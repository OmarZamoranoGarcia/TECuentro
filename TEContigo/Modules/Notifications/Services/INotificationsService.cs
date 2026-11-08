using TEContigo.Modules.Notifications.DTOs;
using TEContigo.Shared.Pagination;

namespace TEContigo.Modules.Notifications.Services;

public interface INotificationsService
{
    Task<PagedResultDto<NotificationDto>> GetAllForCurrentUserAsync(
        int pageNumber,
        int pageSize);

    Task<int> GetUnreadCountAsync();

    Task MarkAsReadAsync(long id);

    Task MarkAllAsReadAsync();

    /// <summary>
    /// Punto de entrada para los demás servicios (Matches, ContactRequests,
    /// Messages, Conversations). Crea la notificación y encola el push.
    /// NUNCA lanza excepciones: notificar es secundario y no debe romper
    /// la operación principal que lo disparó.
    /// </summary>
    /// <param name="collapseIfUnread">
    /// Si es true y el usuario ya tiene una sin leer del mismo tipo y
    /// entidad, no crea otra (se usa para los mensajes nuevos).
    /// </param>
    Task NotifyAsync(
        long userId,
        string type,
        string title,
        string message,
        string referenceType,
        long referenceId,
        bool collapseIfUnread = false);

    /// <summary>
    /// Marca como leídas las notificaciones del usuario que apuntan a
    /// una entidad. Tampoco lanza excepciones.
    /// </summary>
    Task MarkReferenceAsReadAsync(
        long userId,
        string referenceType,
        long referenceId);
}