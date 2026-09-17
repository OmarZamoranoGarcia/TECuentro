using TEContigo.Infrastructure.ImagesStorage;
using TEContigo.Modules.Reports.DTOs;
using TEContigo.Modules.Reports.Models;
using TEContigo.Modules.Reports.Repositories;
using TEContigo.Shared.Security.CurrentUser;

namespace TEContigo.Modules.Reports.Services;

public class ReportsService : IReportsService
{
    private const string RoleAdmin = "ADMIN";
    private const string RoleModerator = "MODERATOR";
    private const string StatusPublished = "Publicado";

    private readonly IReportsRepository _reportsRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IImageService _imageService;

    public ReportsService(
        IReportsRepository reportsRepository,
        ICurrentUserService currentUserService,
        IImageService imageService
    )
    {
        _reportsRepository = reportsRepository;
        _currentUserService = currentUserService;
        _imageService = imageService;
    }

    public async Task<IEnumerable<ReportDto>> GetAllAsync()
    {
        var reports = await _reportsRepository.GetAllAsync();

        var tasks = reports.Select(async report =>
        {
            var photoUrl = await GetPhotoUrlAsync(report.PhotoPath);

            return new ReportDto
            {
                Id = report.Id,
                UserId = report.UserId,
                Name = report.Name,
                Category = report.Category,
                Description = report.Description,
                Status = report.Status,
                PhotoUrl = photoUrl,
                CreatedAt = report.CreatedAt,
                UpdatedAt = report.UpdatedAt
            };
        });

        return await Task.WhenAll(tasks);
    }

    public async Task<ReportDto?> GetByIdAsync(long id)
    {
        var report = await _reportsRepository.GetByIdAsync(id);

        if (report == null)
        {
            return null;
        }

        var photoUrl = await GetPhotoUrlAsync(report.PhotoPath);

        return new ReportDto
        {
            Id = report.Id,
            UserId = report.UserId,
            Name = report.Name,
            Category = report.Category,
            Description = report.Description,
            Status = report.Status,
            PhotoUrl = photoUrl,
            CreatedAt = report.CreatedAt,
            UpdatedAt = report.UpdatedAt
        };
    }

    public async Task<ReportResponseDto> CreateAsync(CreateReportDto dto)
    {
        var userId = _currentUserService.UserId;

        var report = new ReportsModel
        {
            UserId = userId,
            Name = dto.Name,
            Category = dto.Category,
            Description = dto.Description,
            PhotoPath = null,
            Status = StatusPublished,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var reportId =
            await _reportsRepository.CreateAsync(report);

        string? uploadedPhotoPath = null;

        try
        {
            if (dto.Photo != null)
            {
                uploadedPhotoPath =
                    await _imageService.UploadAsync(
                        dto.Photo,
                        $"reports/{reportId}");

                report.Id = reportId;
                report.PhotoPath = uploadedPhotoPath;

                await _reportsRepository.UpdateAsync(report);
            }

            return new ReportResponseDto
            {
                Success = true,
                Message = "Reporte creado correctamente.",
                Id = reportId
            };
        }
        catch
        {
            if (uploadedPhotoPath != null)
            {
                await _imageService.DeleteAsync(
                    uploadedPhotoPath);
            }

            await _reportsRepository.DeleteAsync(
                reportId);

            throw;
        }
    }

    public async Task<ReportResponseDto> UpdateAsync(long id, UpdateReportDto dto)
    {
        var report =
            await _reportsRepository.GetByIdAsync(id);

        if (report == null)
        {
            throw new KeyNotFoundException(
                "El reporte no existe.");
        }

        var userId = _currentUserService.UserId;
        var role = _currentUserService.Role;

        var isOwner = report.UserId == userId;

        var isAdminOrModerator =
            role == RoleAdmin ||
            role == RoleModerator;

        if (!isOwner && !isAdminOrModerator)
        {
            throw new UnauthorizedAccessException(
                "No tienes permisos para modificar este reporte.");
        }

        if (isAdminOrModerator && !isOwner)
        {
            if (dto.Status != null)
            {
                report.Status = dto.Status;
            }
        }
        else
        {
            if (dto.Name != null)
            {
                report.Name = dto.Name;
            }

            if (dto.Category != null)
            {
                report.Category = dto.Category;
            }

            if (dto.Description != null)
            {
                report.Description = dto.Description;
            }

            if (dto.Status != null && isAdminOrModerator)
            {
                report.Status = dto.Status;
            }
        }

        var oldPhotoPath = report.PhotoPath;
        string? newPhotoPath = null;

        try
        {
            if (dto.Photo != null)
            {
                newPhotoPath =
                    await _imageService.UploadAsync(
                        dto.Photo,
                        $"reports/{id}");

                report.PhotoPath = newPhotoPath;
            }

            report.UpdatedAt = DateTime.UtcNow;

            await _reportsRepository.UpdateAsync(report);

            if (newPhotoPath != null &&
                !string.IsNullOrWhiteSpace(oldPhotoPath))
            {
                await _imageService.DeleteAsync(
                    oldPhotoPath);
            }

            return new ReportResponseDto
            {
                Success = true,
                Message = "Reporte actualizado correctamente.",
                Id = report.Id
            };
        }
        catch
        {
            if (newPhotoPath != null)
            {
                await _imageService.DeleteAsync(
                    newPhotoPath);
            }

            throw;
        }
    }

    public async Task<ReportResponseDto> DeleteAsync(long id)
    {
        var report =
            await _reportsRepository.GetByIdAsync(id);

        if (report == null)
        {
            throw new KeyNotFoundException(
                "El reporte no existe.");
        }

        var userId = _currentUserService.UserId;
        var role = _currentUserService.Role;

        var isOwner = report.UserId == userId;

        var isAdminOrModerator =
            role == RoleAdmin ||
            role == RoleModerator;

        if (!isOwner && !isAdminOrModerator)
        {
            throw new UnauthorizedAccessException(
                "No tienes permisos para eliminar este reporte.");
        }

        /*
         * Primero eliminamos el reporte de la base de datos.
         * Si esto falla, no queda nada inconsistente: la imagen
         * en S3 sigue intacta y el reporte tampoco se tocó.
         */
        await _reportsRepository.DeleteAsync(id);

        /*
         * Solo si el borrado en la base de datos tuvo éxito,
         * eliminamos la imagen en S3. Si esto llegara a fallar,
         * el peor caso es una imagen huérfana en S3 (limpiable
         * después con un job), nunca un registro roto en la BD.
         */
        if (!string.IsNullOrWhiteSpace(report.PhotoPath))
        {
            await _imageService.DeleteAsync(
                report.PhotoPath);
        }

        return new ReportResponseDto
        {
            Success = true,
            Message = "Reporte eliminado correctamente.",
            Id = id
        };
    }

    private async Task<string?> GetPhotoUrlAsync(string? photoPath)
    {
        if (string.IsNullOrWhiteSpace(photoPath))
        {
            return null;
        }

        return await _imageService.GetUrlAsync(photoPath);
    }
}