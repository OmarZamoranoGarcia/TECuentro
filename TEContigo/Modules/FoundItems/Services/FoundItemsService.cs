using TEContigo.Modules.FoundItems.DTOs;
using TEContigo.Modules.FoundItems.Models;
using TEContigo.Modules.FoundItems.Repositories;
using TEContigo.Shared.Security;
using TEContigo.Shared.Security.CurrentUser;

namespace TEContigo.Modules.FoundItems.Services;

public class FoundItemsService : IFoundItemsService
{
    private readonly IFoundItemsRepository _foundItemsRepository;
    private readonly ICurrentUserService _currentUserService;

    public FoundItemsService(
        IFoundItemsRepository foundItemsRepository,
        ICurrentUserService currentUserService)
    {
        _foundItemsRepository = foundItemsRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IEnumerable<FoundItemsModel>> GetAllAsync()
    {
        return await _foundItemsRepository.GetAllAsync();
    }

    public async Task<FoundItemsModel?> GetByIdAsync(long id)
    {
        var foundItem =
            await _foundItemsRepository.GetByIdAsync(id);

        if (foundItem == null)
        {
            throw new KeyNotFoundException(
                "La publicación no existe.");
        }

        return foundItem;
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
            PhotoPath = dto.PhotoPath,
            Status = "Activo",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var foundItemId =
            await _foundItemsRepository.CreateAsync(foundItem);

        return new FoundItemResponseDto
        {
            Success = true,
            Message = "Publicación creada correctamente.",
            Id = foundItemId
        };
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
            _currentUserService.IsInRole("ADMIN");

        var isModerator =
            _currentUserService.IsInRole("MODERATOR");

        var isOwner =
            existingFoundItem.UserId == currentUserId;

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
            existingFoundItem.PhotoPath = dto.PhotoPath;
        }
        else
        {
            throw new UnauthorizedAccessException(
                "No tienes permiso para modificar esta publicación.");
        }

        await _foundItemsRepository.UpdateAsync(existingFoundItem);

        return new FoundItemResponseDto
        {
            Success = true,
            Message = "Publicación actualizada correctamente.",
            Id = existingFoundItem.Id
        };
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
            _currentUserService.IsInRole("ADMIN");

        var isModerator =
            _currentUserService.IsInRole("MODERATOR");

        var isOwner =
            existingFoundItem.UserId == currentUserId;

        // El dueño, ADMIN y MODERATOR pueden eliminar.
        if (!isOwner && !isAdmin && !isModerator)
        {
            throw new UnauthorizedAccessException(
                "No tienes permiso para eliminar esta publicación.");
        }

        await _foundItemsRepository.DeleteAsync(id);

        return new FoundItemResponseDto
        {
            Success = true,
            Message = "Publicación eliminada correctamente.",
            Id = existingFoundItem.Id
        };
    }
}