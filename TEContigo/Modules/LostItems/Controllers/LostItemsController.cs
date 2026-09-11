using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TEContigo.Modules.LostItems.DTOs;
using TEContigo.Modules.LostItems.Services;

namespace TEContigo.Modules.LostItems.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LostItemsController : ControllerBase
{
    private readonly ILostItemsService _lostItemsService;

    public LostItemsController(
        ILostItemsService lostItemsService)
    {
        _lostItemsService = lostItemsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLostItems()
    {
        var lostItems =
            await _lostItemsService.GetAllAsync();

        return Ok(lostItems);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetLostItem(long id)
    {
        var lostItem =
            await _lostItemsService.GetByIdAsync(id);

        return Ok(lostItem);
    }

    [HttpPost]
    public async Task<IActionResult> CreateLostItem(
        CreateLostItemDto dto)
    {
        var response =
            await _lostItemsService.CreateAsync(dto);

        return StatusCode(
            StatusCodes.Status201Created,
            response);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateLostItem(
        long id,
        UpdateLostItemDto dto)
    {
        var response =
            await _lostItemsService.UpdateAsync(id, dto);

        return Ok(response);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteLostItem(
        long id)
    {
        var response =
            await _lostItemsService.DeleteAsync(id);

        return Ok(response);
    }
}