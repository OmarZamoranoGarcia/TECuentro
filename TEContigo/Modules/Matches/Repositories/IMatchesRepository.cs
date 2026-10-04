using TEContigo.Modules.Matches.Models;

namespace TEContigo.Modules.Matches.Repositories;

public interface IMatchesRepository
{
    /// <summary>
    /// Trae todos los Matches sin filtrar por usuario. Pensado para
    /// ADMIN/MODERATOR, que deben poder ver cualquier match.
    /// </summary>
    Task<IEnumerable<MatchesModel>> GetAllAsync();

    /// <summary>
    /// Trae todos los Matches donde el usuario es dueño del
    /// LostItem o del FoundItem involucrado (requiere JOIN).
    /// </summary>
    Task<IEnumerable<MatchesModel>> GetAllForUserAsync(long userId);

    Task<MatchesModel?> GetByIdAsync(long id);

    Task<bool> ExistsAsync(long lostItemId, long foundItemId);

    Task<long> CreateAsync(MatchesModel match);

    /// <summary>
    /// Alterna (activa/desactiva) el flag de solicitud de chat del
    /// usuario indicado. No valida el status del match — esa regla
    /// de negocio la aplica el service ANTES de llamar a este método.
    /// </summary>
    Task<(bool LostRequested, bool FoundRequested)> ToggleChatRequestAsync(
        long matchId,
        bool isLostUser);

    /// <summary>
    /// Transición atómica de status: solo aplica el cambio si el status
    /// actual coincide exactamente con "fromStatus". Devuelve true si
    /// la transición se aplicó, false si no (ya estaba en otro status,
    /// por ejemplo porque otra petición concurrente ya lo cambió).
    /// Ahora también la usa ConversationsService (futuro) para cerrar
    /// el Match cuando su Conversation asociada se cierra.
    /// </summary>
    Task<bool> TryTransitionStatusAsync(
        long matchId,
        string fromStatus,
        string toStatus);
}