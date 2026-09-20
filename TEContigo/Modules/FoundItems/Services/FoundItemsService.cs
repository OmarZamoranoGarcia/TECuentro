using TEContigo.Infrastructure.ImagesStorage;
using TEContigo.Modules.FoundItems.DTOs;
using TEContigo.Modules.FoundItems.Models;
using TEContigo.Modules.FoundItems.Repositories;
using TEContigo.Modules.Matches.Services;
using TEContigo.Shared.Security;
using TEContigo.Shared.Security.CurrentUser;

namespace TEContigo.Modules.FoundItems.Services;

public class FoundItemsService : IFoundItemsService
{
    private const string RoleAdmin = "ADMIN";
    private const string RoleModerator = "MODERATOR";
    private const string StatusActive = "Activo";

    private readonly IFoundItemsRepository _foundItemsRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IImageService _imageService;
    private readonly IMatchesService _matchesService;

    public FoundItemsService(
        IFoundItemsRepository foundItemsRepository,
        ICurrentUserService currentUserService,
        IImageService imageService,
        IMatchesService matchesService)
    {
        _foundItemsRepository = foundItemsRepository;
        _currentUserService = currentUserService;
        _imageService = imageService;
        _matchesService = matchesService;
    }

    public async Task<IEnumerable<FoundItemDto>> GetAllAsync()
    {
        var foundItems = await _foundItemsRepository.GetAllAsync();

        var tasks = foundItems.Select(async foundItem =>
        {
            var photoUrl = await GetPhotoUrlAsync(foundItem.PhotoPath);

            return MapToDto(foundItem, photoUrl);
        });

        return await Task.WhenAll(tasks);
    }

    public async Task<FoundItemDto?> GetByIdAsync(long id)
    {
        var foundItem =
            await _foundItemsRepository.GetByIdAsync(id);

        if (foundItem == null)
        {
            throw new KeyNotFoundException(
                "La publicación no existe.");
        }

        var photoUrl = await GetPhotoUrlAsync(foundItem.PhotoPath);

        return MapToDto(foundItem, photoUrl);
    }

    public async Task<FoundItemResponseDto> CreateAsync(
        CreateFoundItemDto dto)
    {
        var foundItem = new FoundItemsModel
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

        var foundItemId =
            await _foundItemsRepository.CreateAsync(foundItem);

        string? uploadedPhotoPath = null;

        try
        {
            if (dto.Photo != null)
            {
                uploadedPhotoPath =
                    await _imageService.UploadAsync(
                        dto.Photo,
                        $"foundItems/{foundItemId}");

                foundItem.Id = foundItemId;
                foundItem.PhotoPath = uploadedPhotoPath;

                await _foundItemsRepository.UpdateAsync(foundItem);
            }

            // El matching se dispara aquí, DESPUÉS de que la publicación
            // ya quedó guardada correctamente (con o sin foto). Si el
            // matching fallara, no queremos perder la publicación ni la
            // imagen ya subida, así que va antes del return y con su
            // propio manejo de errores (no se propaga hacia el catch).
            await SafeGenerateMatchesAsync(foundItemId);

            return new FoundItemResponseDto
            {
                Success = true,
                Message = "Publicación creada correctamente.",
                Id = foundItemId
            };
        }
        catch
        {
            if (uploadedPhotoPath != null)
            {
                await _imageService.DeleteAsync(
                    uploadedPhotoPath);
            }

            await _foundItemsRepository.DeleteAsync(
                foundItemId);

            throw;
        }
    }

    private async Task SafeGenerateMatchesAsync(long foundItemId)
    {
        try
        {
            await _matchesService.GenerateMatchesForFoundItemAsync(foundItemId);
        }
        catch
        {
            // El matching es un proceso secundario: si falla, no debe
            // tumbar la creación de la publicación que ya se guardó
            // correctamente. Aquí es donde, cuando agregues logging
            // (ILogger), deberías registrar el error para investigarlo,
            // en vez de tragártelo en silencio.
        }
    }

    public async Task<FoundItemResponseDto> UpdateAsync(
        long id,
        UpdateFoundItemDto dto)
    {
        var existingFoundItem =
            await _foundItemsRepository.GetByIdAsync(id);

        if (existingFoundItem == null)
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
            existingFoundItem.UserId == currentUserId;

        var oldPhotoPath = existingFoundItem.PhotoPath;
        string? newPhotoPath = null;

        try
        {
            // ADMIN y MODERATOR solamente pueden modificar el status.
            if (isAdmin || isModerator)
            {
                if (string.IsNullOrWhiteSpace(dto.Status))
                {
                    throw new ArgumentException(
                        "Debes proporcionar un status.");
                }

                existingFoundItem.Status = dto.Status;
            }
            // USER solamente puede modificar su propia publicación.
            else if (isOwner)
            {
                existingFoundItem.Category = dto.Category;
                existingFoundItem.Article = dto.Article;
                existingFoundItem.Color = dto.Color;
                existingFoundItem.Location = dto.Location;
                existingFoundItem.Description = dto.Description;

                if (dto.Photo != null)
                {
                    newPhotoPath =
                        await _imageService.UploadAsync(
                            dto.Photo,
                            $"foundItems/{id}");

                    existingFoundItem.PhotoPath = newPhotoPath;
                }
            }
            else
            {
                throw new UnauthorizedAccessException(
                    "No tienes permiso para modificar esta publicación.");
            }

            existingFoundItem.UpdatedAt = DateTime.UtcNow;

            await _foundItemsRepository.UpdateAsync(existingFoundItem);

            if (newPhotoPath != null &&
                !string.IsNullOrWhiteSpace(oldPhotoPath))
            {
                await _imageService.DeleteAsync(
                    oldPhotoPath);
            }

            return new FoundItemResponseDto
            {
                Success = true,
                Message = "Publicación actualizada correctamente.",
                Id = existingFoundItem.Id
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

    public async Task<FoundItemResponseDto> DeleteAsync(long id)
    {
        var existingFoundItem =
            await _foundItemsRepository.GetByIdAsync(id);

        if (existingFoundItem == null)
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
            existingFoundItem.UserId == currentUserId;

        // El dueño, ADMIN y MODERATOR pueden eliminar.
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
        await _foundItemsRepository.DeleteAsync(id);

        /*
         * Solo si el borrado en la base de datos tuvo éxito,
         * eliminamos la imagen en S3. Si esto llegara a fallar,
         * el peor caso es una imagen huérfana en S3, nunca un
         * registro roto en la base de datos.
         */
        if (!string.IsNullOrWhiteSpace(existingFoundItem.PhotoPath))
        {
            await _imageService.DeleteAsync(
                existingFoundItem.PhotoPath);
        }

        return new FoundItemResponseDto
        {
            Success = true,
            Message = "Publicación eliminada correctamente.",
            Id = existingFoundItem.Id
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

    private static FoundItemDto MapToDto(
        FoundItemsModel foundItem,
        string? photoUrl)
    {
        return new FoundItemDto
        {
            Id = foundItem.Id,
            UserId = foundItem.UserId,
            Category = foundItem.Category,
            Article = foundItem.Article,
            Color = foundItem.Color,
            Location = foundItem.Location,
            Description = foundItem.Description,
            PhotoUrl = photoUrl,
            Status = foundItem.Status,
            CreatedAt = foundItem.CreatedAt,
            UpdatedAt = foundItem.UpdatedAt
        };
    }
}