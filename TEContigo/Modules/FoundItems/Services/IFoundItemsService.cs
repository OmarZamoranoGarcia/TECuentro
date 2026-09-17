using TEContigo.Modules.FoundItems.DTOs;
using TEContigo.Modules.FoundItems.Models;

namespace TEContigo.Modules.FoundItems.Services
{
    public interface IFoundItemsService
    {
        Task<IEnumerable<FoundItemDto>> GetAllAsync();

        Task<FoundItemDto?> GetByIdAsync(long id);

        Task<FoundItemResponseDto> CreateAsync(CreateFoundItemDto dto);

        Task<FoundItemResponseDto> UpdateAsync(long id, UpdateFoundItemDto dto);

        Task<FoundItemResponseDto> DeleteAsync(long id);
    }
}
