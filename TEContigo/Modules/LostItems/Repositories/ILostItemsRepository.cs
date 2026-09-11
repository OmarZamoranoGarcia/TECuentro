using TEContigo.Modules.LostItems.Models;

namespace TEContigo.Modules.LostItems.Repositories
{
    public interface ILostItemsRepository
    {
        Task<IEnumerable<LostItemsModel>> GetAllAsync();
        Task<LostItemsModel?> GetByIdAsync(long id);
        Task<long> CreateAsync(LostItemsModel lostItem);
        Task UpdateAsync(LostItemsModel lostItem);
        Task DeleteAsync(long id);
    }
}
