using TEContigo.Modules.LostItems.DTOs;
using TEContigo.Modules.LostItems.Models;
using TEContigo.Modules.LostItems.Repositories;
using TEContigo.Shared.Security;
using TEContigo.Shared.Security.CurrentUser;

namespace TEContigo.Modules.LostItems.Services;

public class LostItemsService : ILostItemsService
{
    private readonly ILostItemsRepository _lostItemsRepository;
    private readonly ICurrentUserService _currentUserService;

    public LostItemsService(
        ILostItemsRepository lostItemsRepository,
        ICurrentUserService currentUserService)
    {
        _lostItemsRepository = lostItemsRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IEnumerable<LostItemsModel>> GetAllAsync()
    {
        return await _lostItemsRepository.GetAllAsync();
    }

    public async Task<LostItemsModel?> GetByIdAsync(long id)
    {
        var lostItem =
            await _lostItemsRepository.GetByIdAsync(id);

        if (lostItem == null)
        {
            throw new KeyNotFoundException(
                "La publicación no existe.");
        }

        return lostItem;
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
            PhotoPath = dto.PhotoPath,
            Status = "Activo",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var lostItemId =
            await _lostItemsRepository.CreateAsync(lostItem);

        return new LostItemResponseDto
        {
            Success = true,
            Message = "Publicación creada correctamente.",
            Id = lostItemId
        };
    }

    public async Task<LostItemResponseDto> UpdateAsync(long id,UpdateLostItemDto dto)
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
            _currentUserService.IsInRole("ADMIN");

        var isModerator =
            _currentUserService.IsInRole("MODERATOR");

        var isOwner =
            existingLostItem.UserId == currentUserId;

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
            existingLostItem.PhotoPath = dto.PhotoPath;
        }
        else
        {
            throw new UnauthorizedAccessException(
                "No tienes permiso para modificar esta publicación.");
        }

        await _lostItemsRepository.UpdateAsync(existingLostItem);

        return new LostItemResponseDto
        {
            Success = true,
            Message = "Publicación actualizada correctamente.",
            Id = existingLostItem.Id
        };
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
            _currentUserService.IsInRole("ADMIN");

        var isModerator =
            _currentUserService.IsInRole("MODERATOR");

        var isOwner =
            existingLostItem.UserId == currentUserId;

        if (!isOwner && !isAdmin && !isModerator)
        {
            throw new UnauthorizedAccessException(
                "No tienes permiso para eliminar esta publicación.");
        }

        await _lostItemsRepository.DeleteAsync(id);

        return new LostItemResponseDto
        {
            Success = true,
            Message = "Publicación eliminada correctamente.",
            Id = existingLostItem.Id
        };
    }
}