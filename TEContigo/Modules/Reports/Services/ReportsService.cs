using TEContigo.Modules.Reports.DTOs;
using TEContigo.Modules.Reports.Models;
using TEContigo.Modules.Reports.Repositories;
using TEContigo.Shared.Security.CurrentUser;

namespace TEContigo.Modules.Reports.Services;

public class ReportsService : IReportsService
{
    private readonly IReportsRepository _reportsRepository;
    private readonly ICurrentUserService _currentUserService;

    public ReportsService(IReportsRepository reportsRepository, ICurrentUserService currentUserService)
    {
        _reportsRepository = reportsRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IEnumerable<ReportsModel>> GetAllAsync()
    {
        return await _reportsRepository.GetAllAsync();
    }

    public async Task<ReportsModel?> GetByIdAsync(long id)
    {
        var report = await _reportsRepository.GetByIdAsync(id);

        if (report == null)
        {
            throw new KeyNotFoundException(
                "El reporte no existe.");
        }

        return report;
    }

    public async Task<ReportResponseDto> CreateAsync(CreateReportDto dto)
    {
        var report = new ReportsModel
        {
            UserId = _currentUserService.UserId,
            Name = dto.Name,
            Category = dto.Category,
            PhotoPath = dto.PhotoPath,
            Description = dto.Description,
            Status = "Publicado",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var reportId =
            await _reportsRepository.CreateAsync(report);

        return new ReportResponseDto
        {
            Success = true,
            Message = "Reporte creado correctamente.",
            Id = reportId
        };
    }

    public async Task<ReportResponseDto> UpdateAsync(long id,UpdateReportDto dto)
    {
        var existingReport =
            await _reportsRepository.GetByIdAsync(id);

        if (existingReport == null)
        {
            throw new KeyNotFoundException(
                "El reporte no existe.");
        }

        var currentUserId = _currentUserService.UserId;

        var isAdmin =
            _currentUserService.IsInRole("ADMIN");

        var isModerator =
            _currentUserService.IsInRole("MODERATOR");

        var isOwner =
            existingReport.UserId == currentUserId;

        if (!isOwner && !isAdmin && !isModerator)
        {
            throw new UnauthorizedAccessException(
                "No tienes permiso para modificar este reporte.");
        }

        existingReport.Name = dto.Name;
        existingReport.Category = dto.Category;
        existingReport.PhotoPath = dto.PhotoPath;
        existingReport.Description = dto.Description;

        if (isAdmin || isModerator)
        {
            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                existingReport.Status = dto.Status;
            }
        }

        await _reportsRepository.UpdateAsync(existingReport);

        return new ReportResponseDto
        {
            Success = true,
            Message = "Reporte actualizado correctamente.",
            Id = existingReport.Id
        };
    }

    public async Task<ReportResponseDto> DeleteAsync(long id)
    {
        var existingReport =
            await _reportsRepository.GetByIdAsync(id);

        if (existingReport == null)
        {
            throw new KeyNotFoundException(
                "El reporte no existe.");
        }

        var currentUserId = _currentUserService.UserId;

        var isAdmin =
            _currentUserService.IsInRole("ADMIN");

        var isModerator =
            _currentUserService.IsInRole("MODERATOR");

        var isOwner =
            existingReport.UserId == currentUserId;

        if (!isOwner && !isAdmin && !isModerator)
        {
            throw new UnauthorizedAccessException(
                "No tienes permiso para eliminar este reporte.");
        }

        await _reportsRepository.DeleteAsync(id);

        return new ReportResponseDto
        {
            Success = true,
            Message = "Reporte eliminado correctamente.",
            Id = existingReport.Id
        };
    }
}