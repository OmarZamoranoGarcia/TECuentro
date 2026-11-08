using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TEContigo.Modules.Devices.DTOs;
using TEContigo.Modules.Devices.Services;

namespace TEContigo.Modules.Devices.Controllers;

[ApiController]
[Route("api/devices")]
[Authorize]
public class DevicesController : ControllerBase
{
    private readonly IDevicesService _devicesService;

    public DevicesController(IDevicesService devicesService)
    {
        _devicesService = devicesService;
    }

    // POST /api/devices   { "pushToken": "ExponentPushToken[...]", "platform": "android" }
    [HttpPost]
    public async Task<IActionResult> Register(RegisterDeviceDto dto)
    {
        var result = await _devicesService.RegisterAsync(dto);

        return Ok(result);
    }

    // POST /api/devices/unregister   { "pushToken": "ExponentPushToken[...]" }
    [HttpPost("unregister")]
    public async Task<IActionResult> Unregister(UnregisterDeviceDto dto)
    {
        var result = await _devicesService.UnregisterAsync(dto);

        return Ok(result);
    }
}