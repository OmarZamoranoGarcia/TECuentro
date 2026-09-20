using TEContigo.Modules.Matches.DTOs;

namespace TEContigo.Modules.Matches.Services
{
    public interface IMatchesService
    {
        Task<IEnumerable<MatchDto>> GetAllForCurrentUserAsync();
        Task<MatchDto?> GetByIdAsync(long id);

        /// <summary>
        /// Compara un LostItem recién creado contra todos los FoundItems
        /// activos y crea los Matches que superen el umbral. Se llama
        /// desde LostItemsService.CreateAsync justo después de crear el registro.
        /// </summary>
        Task GenerateMatchesForLostItemAsync(long lostItemId);

        /// <summary>
        /// Simétrico: compara un FoundItem recién creado contra todos los
        /// LostItems activos. Se llama desde FoundItemsService.CreateAsync.
        /// </summary>
        Task GenerateMatchesForFoundItemAsync(long foundItemId);

        Task<MatchResponseDto> RequestChatAsync(long matchId);
        Task<MatchResponseDto> ConfirmReturnAsync(long matchId);
    }
}
