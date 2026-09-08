using TEContigo.Modules.Reports.Models;

namespace TEContigo.Modules.Reports.Repositories
{
    public interface IReportsRepository
    {
        Task<IEnumerable<ReportsModel>> GetAllAsync();

        Task<ReportsModel?> GetByIdAsync(long id);

        Task<long> CreateAsync(ReportsModel report);

        Task UpdateAsync(ReportsModel report);

        Task DeleteAsync(long id);
    }
}
