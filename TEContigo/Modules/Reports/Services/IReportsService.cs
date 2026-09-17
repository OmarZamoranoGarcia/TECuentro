using TEContigo.Modules.Reports.DTOs;
using TEContigo.Modules.Reports.Models;

namespace TEContigo.Modules.Reports.Services
{
    public interface IReportsService
    {
        Task<IEnumerable<ReportDto>> GetAllAsync();

        Task<ReportDto?> GetByIdAsync(long id);

        Task<ReportResponseDto> CreateAsync(CreateReportDto dto);

        Task<ReportResponseDto> UpdateAsync(long id, UpdateReportDto dto);

        Task<ReportResponseDto> DeleteAsync(long id);
    }
}
