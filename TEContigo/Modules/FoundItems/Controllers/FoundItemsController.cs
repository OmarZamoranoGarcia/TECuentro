using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TEContigo.Modules.FoundItems.DTOs;
using TEContigo.Modules.FoundItems.Services;

namespace TEContigo.Modules.FoundItems.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FoundItemsController : ControllerBase
{
    private readonly IFoundItemsService _foundItemsService;

    public FoundItemsController(
        IFoundItemsService foundItemsService)
    {
        _foundItemsService = foundItemsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetFoundItems()
    {
        var foundItems =
            await _foundItemsService.GetAllAsync();

        return Ok(foundItems);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetFoundItem(long id)
    {
        var foundItem =
            await _foundItemsService.GetByIdAsync(id);

        return Ok(foundItem);
    }

    [HttpPost]
    public async Task<IActionResult> CreateFoundItem(
        CreateFoundItemDto dto)
    {
        var response =
            await _foundItemsService.CreateAsync(dto);

        return StatusCode(
            StatusCodes.Status201Created,
            response);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateFoundItem(
        long id,
        UpdateFoundItemDto dto)
    {
        var response =
            await _foundItemsService.UpdateAsync(id, dto);

        return Ok(response);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteFoundItem(long id)
    {
        var response =
            await _foundItemsService.DeleteAsync(id);

        return Ok(response);
    }
}