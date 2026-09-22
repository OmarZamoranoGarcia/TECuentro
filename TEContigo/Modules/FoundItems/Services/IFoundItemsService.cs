using TEContigo.Modules.FoundItems.DTOs;
using TEContigo.Modules.FoundItems.Models;
using TEContigo.Shared.Pagination;

namespace TEContigo.Modules.FoundItems.Services
{
    public interface IFoundItemsService
    {
        Task<PagedResultDto<FoundItemDto>> GetAllAsync(int pageNumber, int pageSize);

        Task<FoundItemDto?> GetByIdAsync(long id);

        Task<FoundItemResponseDto> CreateAsync(CreateFoundItemDto dto);

        Task<FoundItemResponseDto> UpdateAsync(long id, UpdateFoundItemDto dto);

        Task<FoundItemResponseDto> DeleteAsync(long id);
    }
}
