using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TEContigo.Modules.Reports.DTOs;
using TEContigo.Modules.Reports.Services;
using TEContigo.Shared.Pagination;

namespace TEContigo.Modules.Reports.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportsService _reportsService;

    public ReportsController(IReportsService reportsService)
    {
        _reportsService = reportsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationQueryDto query)
    {
        var result = await _reportsService.GetAllAsync(query.PageNumber, query.PageSize);

        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetReport(long id)
    {
        var report = await _reportsService.GetByIdAsync(id);

        return Ok(report);
    }

    [HttpPost]
    public async Task<IActionResult> CreateReport(CreateReportDto dto)
    {
        var response = await _reportsService.CreateAsync(dto);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateReport(
    long id,
    UpdateReportDto dto)
    {
        var response = await _reportsService.UpdateAsync(id, dto);

        return Ok(response);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteReport(long id)
    {
        var response = await _reportsService.DeleteAsync(id);

        return Ok(response);
    }
}