using TEContigo.Modules.Devices.DTOs;

namespace TEContigo.Modules.Devices.Services;

public interface IDevicesService
{
    Task<DeviceResponseDto> RegisterAsync(RegisterDeviceDto dto);
    Task<DeviceResponseDto> UnregisterAsync(UnregisterDeviceDto dto);
}