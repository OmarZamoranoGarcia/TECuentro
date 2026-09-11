using TEContigo.Modules.FoundItems.DTOs;
using TEContigo.Modules.FoundItems.Models;

namespace TEContigo.Modules.FoundItems.Services
{
    public interface IFoundItemsService
    {
        Task<IEnumerable<FoundItemsModel>> GetAllAsync();

        Task<FoundItemsModel?> GetByIdAsync(long id);

        Task<FoundItemResponseDto> CreateAsync(
            CreateFoundItemDto dto);

        Task<FoundItemResponseDto> UpdateAsync(
            long id,
            UpdateFoundItemDto dto);

        Task<FoundItemResponseDto> DeleteAsync(long id);
    }
}
