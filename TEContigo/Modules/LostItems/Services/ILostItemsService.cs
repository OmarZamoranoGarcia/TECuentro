using TEContigo.Modules.LostItems.DTOs;
using TEContigo.Modules.LostItems.Models;
using TEContigo.Shared.Pagination;

namespace TEContigo.Modules.LostItems.Services
{
    public interface ILostItemsService
    {
        Task<PagedResultDto<LostItemDto>> GetAllAsync(int pageNumber, int pageSize);

        Task<LostItemDto?> GetByIdAsync(long id);

        Task<LostItemResponseDto> CreateAsync(CreateLostItemDto dto);

        Task<LostItemResponseDto> UpdateAsync(long id, UpdateLostItemDto dto);

        Task<LostItemResponseDto> DeleteAsync(long id);
    }
}
