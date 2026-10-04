using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TEContigo.Modules.Conversations.Services;

namespace TEContigo.Modules.Conversations.Controllers;

[ApiController]
[Route("api/conversations")]
[Authorize]
public class ConversationsController : ControllerBase
{
    private readonly IConversationsService _conversationsService;

    public ConversationsController(IConversationsService conversationsService)
    {
        _conversationsService = conversationsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _conversationsService.GetAllForCurrentUserAsync();

        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _conversationsService.GetByIdAsync(id);

        return Ok(result);
    }

    [HttpPost("{id:long}/confirm-return")]
    public async Task<IActionResult> ConfirmReturn(long id)
    {
        var result = await _conversationsService.ConfirmReturnAsync(id);

        return Ok(result);
    }
}