using TEContigo.Infrastructure.ImagesStorage;
using TEContigo.Modules.LostItems.DTOs;
using TEContigo.Modules.LostItems.Models;
using TEContigo.Modules.LostItems.Repositories;
using TEContigo.Shared.Security;
using TEContigo.Shared.Security.CurrentUser;

namespace TEContigo.Modules.LostItems.Services;

public class LostItemsService : ILostItemsService
{
    private const string RoleAdmin = "ADMIN";
    private const string RoleModerator = "MODERATOR";
    private const string StatusActive = "Activo";

    private readonly ILostItemsRepository _lostItemsRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IImageService _imageService;

    public LostItemsService(
        ILostItemsRepository lostItemsRepository,
        ICurrentUserService currentUserService,
        IImageService imageService)
    {
        _lostItemsRepository = lostItemsRepository;
        _currentUserService = currentUserService;
        _imageService = imageService;
    }

    public async Task<IEnumerable<LostItemDto>> GetAllAsync()
    {
        var lostItems = await _lostItemsRepository.GetAllAsync();

        var tasks = lostItems.Select(async lostItem =>
        {
            var photoUrl = await GetPhotoUrlAsync(lostItem.PhotoPath);

            return MapToDto(lostItem, photoUrl);
        });

        return await Task.WhenAll(tasks);
    }

    public async Task<LostItemDto?> GetByIdAsync(long id)
    {
        var lostItem =
            await _lostItemsRepository.GetByIdAsync(id);

        if (lostItem == null)
        {
            throw new KeyNotFoundException(
                "La publicación no existe.");
        }

        var photoUrl = await GetPhotoUrlAsync(lostItem.PhotoPath);

        return MapToDto(lostItem, photoUrl);
    }

    public async Task<LostItemResponseDto> CreateAsync(CreateLostItemDto dto)
    {
        var lostItem = new LostItemsModel
        {
            UserId = _currentUserService.UserId,
            Category = dto.Category,
            Article = dto.Article,
            Color = dto.Color,
            Location = dto.Location,
            Description = dto.Description,
            PhotoPath = null,
            Status = StatusActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var lostItemId =
            await _lostItemsRepository.CreateAsync(lostItem);

        string? uploadedPhotoPath = null;

        try
        {
            if (dto.Photo != null)
            {
                uploadedPhotoPath =
                    await _imageService.UploadAsync(
                        dto.Photo,
                        $"lostItems/{lostItemId}");

                lostItem.Id = lostItemId;
                lostItem.PhotoPath = uploadedPhotoPath;

                await _lostItemsRepository.UpdateAsync(lostItem);
            }

            return new LostItemResponseDto
            {
                Success = true,
                Message = "Publicación creada correctamente.",
                Id = lostItemId
            };
        }
        catch
        {
            if (uploadedPhotoPath != null)
            {
                await _imageService.DeleteAsync(
                    uploadedPhotoPath);
            }

            await _lostItemsRepository.DeleteAsync(
                lostItemId);

            throw;
        }
    }

    public async Task<LostItemResponseDto> UpdateAsync(long id, UpdateLostItemDto dto)
    {
        var existingLostItem =
            await _lostItemsRepository.GetByIdAsync(id);

        if (existingLostItem == null)
        {
            throw new KeyNotFoundException(
                "La publicación no existe.");
        }

        var currentUserId =
            _currentUserService.UserId;

        var isAdmin =
            _currentUserService.IsInRole(RoleAdmin);

        var isModerator =
            _currentUserService.IsInRole(RoleModerator);

        var isOwner =
            existingLostItem.UserId == currentUserId;

        var oldPhotoPath = existingLostItem.PhotoPath;
        string? newPhotoPath = null;

        try
        {
            if (isAdmin || isModerator)
            {
                if (string.IsNullOrWhiteSpace(dto.Status))
                {
                    throw new ArgumentException(
                        "Debes proporcionar un status.");
                }

                existingLostItem.Status = dto.Status;
            }
            else if (isOwner)
            {
                existingLostItem.Category = dto.Category;
                existingLostItem.Article = dto.Article;
                existingLostItem.Color = dto.Color;
                existingLostItem.Location = dto.Location;
                existingLostItem.Description = dto.Description;

                if (dto.Photo != null)
                {
                    newPhotoPath =
                        await _imageService.UploadAsync(
                            dto.Photo,
                            $"lostItems/{id}");

                    existingLostItem.PhotoPath = newPhotoPath;
                }
            }
            else
            {
                throw new UnauthorizedAccessException(
                    "No tienes permiso para modificar esta publicación.");
            }

            existingLostItem.UpdatedAt = DateTime.UtcNow;

            await _lostItemsRepository.UpdateAsync(existingLostItem);

            if (newPhotoPath != null &&
                !string.IsNullOrWhiteSpace(oldPhotoPath))
            {
                await _imageService.DeleteAsync(
                    oldPhotoPath);
            }

            return new LostItemResponseDto
            {
                Success = true,
                Message = "Publicación actualizada correctamente.",
                Id = existingLostItem.Id
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

    public async Task<LostItemResponseDto> DeleteAsync(long id)
    {
        var existingLostItem =
            await _lostItemsRepository.GetByIdAsync(id);

        if (existingLostItem == null)
        {
            throw new KeyNotFoundException(
                "La publicación no existe.");
        }

        var currentUserId =
            _currentUserService.UserId;

        var isAdmin =
            _currentUserService.IsInRole(RoleAdmin);

        var isModerator =
            _currentUserService.IsInRole(RoleModerator);

        var isOwner =
            existingLostItem.UserId == currentUserId;

        if (!isOwner && !isAdmin && !isModerator)
        {
            throw new UnauthorizedAccessException(
                "No tienes permiso para eliminar esta publicación.");
        }

        /*
         * Primero eliminamos la publicación de la base de datos.
         * Si esto falla, la imagen en S3 sigue intacta y no queda
         * nada inconsistente.
         */
        await _lostItemsRepository.DeleteAsync(id);

        /*
         * Solo si el borrado en la base de datos tuvo éxito,
         * eliminamos la imagen en S3. Si esto llegara a fallar,
         * el peor caso es una imagen huérfana en S3, nunca un
         * registro roto en la base de datos.
         */
        if (!string.IsNullOrWhiteSpace(existingLostItem.PhotoPath))
        {
            await _imageService.DeleteAsync(
                existingLostItem.PhotoPath);
        }

        return new LostItemResponseDto
        {
            Success = true,
            Message = "Publicación eliminada correctamente.",
            Id = existingLostItem.Id
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

    private static LostItemDto MapToDto(
        LostItemsModel lostItem,
        string? photoUrl)
    {
        return new LostItemDto
        {
            Id = lostItem.Id,
            UserId = lostItem.UserId,
            Category = lostItem.Category,
            Article = lostItem.Article,
            Color = lostItem.Color,
            Location = lostItem.Location,
            Description = lostItem.Description,
            PhotoUrl = photoUrl,
            Status = lostItem.Status,
            CreatedAt = lostItem.CreatedAt,
            UpdatedAt = lostItem.UpdatedAt
        };
    }
}