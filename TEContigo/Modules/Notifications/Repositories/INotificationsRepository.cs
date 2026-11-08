using TEContigo.Modules.Notifications.Models;
using TEContigo.Shared.Pagination;

namespace TEContigo.Modules.Notifications.Repositories;

public interface INotificationsRepository
{
    /// <summary>
    /// Más recientes primero.
    /// </summary>
    Task<PagedResultDto<NotificationsModel>> GetByUserIdAsync(
        long userId,
        int pageNumber,
        int pageSize);

    Task<int> CountUnreadAsync(long userId);

    /// <summary>
    /// Sirve para agrupar: saber si ya hay una sin leer del mismo tipo
    /// y la misma entidad antes de crear otra.
    /// </summary>
    Task<bool> HasUnreadAsync(
        long userId,
        string type,
        string referenceType,
        long referenceId);

    Task<long> CreateAsync(NotificationsModel notification);

    /// <summary>
    /// Solo marca si la notificación es del usuario. Devuelve false si
    /// no existe o es de otra persona.
    /// </summary>
    Task<bool> MarkAsReadAsync(long id, long userId);

    Task<int> MarkAllAsReadAsync(long userId);

    /// <summary>
    /// Marca como leídas todas las que apuntan a una entidad (por ejemplo,
    /// al abrir un chat se limpian las de esa conversación).
    /// </summary>
    Task<int> MarkReferenceAsReadAsync(
        long userId,
        string referenceType,
        long referenceId);
}