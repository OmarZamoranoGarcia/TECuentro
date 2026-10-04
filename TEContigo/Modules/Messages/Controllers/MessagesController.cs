using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TEContigo.Modules.Messages.DTOs;
using TEContigo.Modules.Messages.Services;
using TEContigo.Shared.Pagination;

namespace TEContigo.Modules.Messages.Controllers;

[ApiController]
[Route("api/conversations/{conversationId:long}/messages")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMessagesService _messagesService;

    public MessagesController(IMessagesService messagesService)
    {
        _messagesService = messagesService;
    }

    // GET /api/conversations/5/messages?pageNumber=1&pageSize=20
    [HttpGet]
    public async Task<IActionResult> GetMessages(
        long conversationId,
        [FromQuery] PaginationQueryDto query)
    {
        var result = await _messagesService.GetMessagesAsync(
            conversationId, query.PageNumber, query.PageSize);

        return Ok(result);
    }

    // POST /api/conversations/5/messages
    [HttpPost]
    public async Task<IActionResult> SendMessage(
        long conversationId,
        CreateMessageDto dto)
    {
        var result = await _messagesService.SendMessageAsync(conversationId, dto);

        return Ok(result);
    }

    // POST /api/conversations/5/messages/mark-read
    [HttpPost("mark-read")]
    public async Task<IActionResult> MarkAsRead(long conversationId)
    {
        await _messagesService.MarkAsReadAsync(conversationId);

        return Ok(new { Success = true });
    }
}