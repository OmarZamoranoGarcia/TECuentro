using TEContigo.Modules.LostItems.DTOs;
using TEContigo.Modules.LostItems.Models;

namespace TEContigo.Modules.LostItems.Services
{
    public interface ILostItemsService
    {
        Task<IEnumerable<LostItemDto>> GetAllAsync();

        Task<LostItemDto?> GetByIdAsync(long id);

        Task<LostItemResponseDto> CreateAsync(CreateLostItemDto dto);

        Task<LostItemResponseDto> UpdateAsync(long id, UpdateLostItemDto dto);

        Task<LostItemResponseDto> DeleteAsync(long id);
    }
}
