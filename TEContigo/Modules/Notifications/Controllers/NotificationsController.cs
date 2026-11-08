using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TEContigo.Modules.Notifications.DTOs;
using TEContigo.Modules.Notifications.Services;
using TEContigo.Shared.Pagination;

namespace TEContigo.Modules.Notifications.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationsService _notificationsService;

    public NotificationsController(INotificationsService notificationsService)
    {
        _notificationsService = notificationsService;
    }

    // GET /api/notifications?pageNumber=1&pageSize=20
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationQueryDto query)
    {
        var result = await _notificationsService.GetAllForCurrentUserAsync(
            query.PageNumber, query.PageSize);

        return Ok(result);
    }

    // GET /api/notifications/unread-count
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var count = await _notificationsService.GetUnreadCountAsync();

        return Ok(new UnreadCountDto { Count = count });
    }

    // POST /api/notifications/5/read
    [HttpPost("{id:long}/read")]
    public async Task<IActionResult> MarkAsRead(long id)
    {
        await _notificationsService.MarkAsReadAsync(id);

        return Ok(new NotificationResponseDto
        {
            Success = true,
            Message = "Notificación marcada como leída."
        });
    }

    // POST /api/notifications/read-all
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _notificationsService.MarkAllAsReadAsync();

        return Ok(new NotificationResponseDto
        {
            Success = true,
            Message = "Todas las notificaciones se marcaron como leídas."
        });
    }
}