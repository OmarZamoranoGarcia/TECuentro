using TEContigo.Modules.FoundItems.Models;

namespace TEContigo.Modules.FoundItems.Repositories
{
    public interface IFoundItemsRepository
    {
        Task<IEnumerable<FoundItemsModel>> GetAllAsync();

        Task<FoundItemsModel?> GetByIdAsync(long id);

        Task<long> CreateAsync(FoundItemsModel foundItem);

        Task UpdateAsync(FoundItemsModel foundItem);

        Task DeleteAsync(long id);
    }
}
