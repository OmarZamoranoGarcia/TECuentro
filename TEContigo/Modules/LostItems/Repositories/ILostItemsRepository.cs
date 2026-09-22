using TEContigo.Modules.LostItems.Models;
using TEContigo.Shared.Pagination;

namespace TEContigo.Modules.LostItems.Repositories
{
    public interface ILostItemsRepository
    {
        Task<IEnumerable<LostItemsModel>> GetActiveAsync();
        Task<PagedResultDto<LostItemsModel>> GetAllAsync(int pageNumber, int pageSize);
        Task<LostItemsModel?> GetByIdAsync(long id);
        Task<long> CreateAsync(LostItemsModel lostItem);
        Task UpdateAsync(LostItemsModel lostItem);
        Task DeleteAsync(long id);
    }
}
