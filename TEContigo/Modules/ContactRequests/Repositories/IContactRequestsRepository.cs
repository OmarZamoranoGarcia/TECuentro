using TEContigo.Modules.ContactRequests.Models;

namespace TEContigo.Modules.ContactRequests.Repositories;

public interface IContactRequestsRepository
{
    /// <summary>
    /// Sin filtrar. Pensado para ADMIN/MODERATOR.
    /// </summary>
    Task<IEnumerable<ContactRequestsModel>> GetAllAsync();

    /// <summary>
    /// Solicitudes donde el usuario es el requester, o el dueño del
    /// FoundItem al que apunta la solicitud (requiere JOIN).
    /// </summary>
    Task<IEnumerable<ContactRequestsModel>> GetAllForUserAsync(long userId);

    Task<ContactRequestsModel?> GetByIdAsync(long id);

    /// <summary>
    /// Evita que el mismo usuario spamee solicitudes duplicadas sobre
    /// el mismo FoundItem mientras ya tiene una Pendiente.
    /// </summary>
    Task<bool> ExistsPendingAsync(long foundItemId, long requesterId);

    Task<long> CreateAsync(ContactRequestsModel contactRequest);

    /// <summary>
    /// Update atómico condicionado: solo aplica si el status actual
    /// coincide con "fromStatus" (normalmente "Pendiente"). Cubre
    /// aceptar, rechazar y cancelar con el mismo método, evitando que
    /// una solicitud ya respondida se pueda volver a responder.
    /// </summary>
    Task<bool> TryRespondAsync(
        long id,
        string fromStatus,
        string toStatus,
        DateTime respondedAt);
}