using TEContigo.Modules.FoundItems.Models;
using TEContigo.Shared.Pagination;

namespace TEContigo.Modules.FoundItems.Repositories
{
    public interface IFoundItemsRepository
    {
        Task<IEnumerable<FoundItemsModel>> GetActiveAsync();

        Task<PagedResultDto<FoundItemsModel>> GetAllAsync(int pageNumber, int pageSize);

        Task<FoundItemsModel?> GetByIdAsync(long id);

        Task<long> CreateAsync(FoundItemsModel foundItem);

        Task UpdateAsync(FoundItemsModel foundItem);

        Task DeleteAsync(long id);
    }
}
