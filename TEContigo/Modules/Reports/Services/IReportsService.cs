using TEContigo.Modules.Reports.DTOs;
using TEContigo.Modules.Reports.Models;
using TEContigo.Shared.Pagination;

namespace TEContigo.Modules.Reports.Services
{
    public interface IReportsService
    {
        Task<PagedResultDto<ReportDto>> GetAllAsync(int pageNumber, int pageSize);

        Task<ReportDto?> GetByIdAsync(long id);

        Task<ReportResponseDto> CreateAsync(CreateReportDto dto);

        Task<ReportResponseDto> UpdateAsync(long id, UpdateReportDto dto);

        Task<ReportResponseDto> DeleteAsync(long id);
    }
}
