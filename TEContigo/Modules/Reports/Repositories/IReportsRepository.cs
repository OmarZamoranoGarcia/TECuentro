using TEContigo.Modules.Reports.Models;
using TEContigo.Shared.Pagination;

namespace TEContigo.Modules.Reports.Repositories
{
    public interface IReportsRepository
    {
        Task<PagedResultDto<ReportsModel>> GetAllAsync(int pageNumber, int pageSize);

        Task<ReportsModel?> GetByIdAsync(long id);

        Task<long> CreateAsync(ReportsModel report);

        Task UpdateAsync(ReportsModel report);

        Task DeleteAsync(long id);
    }
}
