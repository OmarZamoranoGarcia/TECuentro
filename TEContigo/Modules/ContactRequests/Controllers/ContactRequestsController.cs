using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TEContigo.Modules.ContactRequests.DTOs;
using TEContigo.Modules.ContactRequests.Services;

namespace TEContigo.Modules.ContactRequests.Controllers;

[ApiController]
[Route("api/contactrequests")]
[Authorize]
public class ContactRequestsController : ControllerBase
{
    private readonly IContactRequestsService _contactRequestsService;

    public ContactRequestsController(IContactRequestsService contactRequestsService)
    {
        _contactRequestsService = contactRequestsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _contactRequestsService.GetAllForCurrentUserAsync();

        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _contactRequestsService.GetByIdAsync(id);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateContactRequestDto dto)
    {
        var result = await _contactRequestsService.CreateAsync(dto);

        return Ok(result);
    }

    [HttpPost("{id:long}/accept")]
    public async Task<IActionResult> Accept(long id)
    {
        var result = await _contactRequestsService.AcceptAsync(id);

        return Ok(result);
    }

    [HttpPost("{id:long}/reject")]
    public async Task<IActionResult> Reject(long id)
    {
        var result = await _contactRequestsService.RejectAsync(id);

        return Ok(result);
    }

    [HttpPost("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id)
    {
        var result = await _contactRequestsService.CancelAsync(id);

        return Ok(result);
    }
}