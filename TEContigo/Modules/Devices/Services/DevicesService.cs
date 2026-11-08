using System.Text.RegularExpressions;
using TEContigo.Modules.Devices.DTOs;
using TEContigo.Modules.Devices.Repositories;
using TEContigo.Shared.Security.CurrentUser;

namespace TEContigo.Modules.Devices.Services;

public class DevicesService : IDevicesService
{
    // Los push solo existen en móvil; "web" no se acepta.
    private static readonly HashSet<string> AllowedPlatforms = new() { "ios", "android" };

    // ExponentPushToken[xxxx] (formato actual) o ExpoPushToken[xxxx].
    private static readonly Regex ExpoPushTokenRegex = new(
        @"^(Exponent|Expo)PushToken\[[^\]\s]+\]$",
        RegexOptions.Compiled);

    private readonly IUserDevicesRepository _devicesRepository;
    private readonly ICurrentUserService _currentUserService;

    public DevicesService(
        IUserDevicesRepository devicesRepository,
        ICurrentUserService currentUserService)
    {
        _devicesRepository = devicesRepository;
        _currentUserService = currentUserService;
    }

    public async Task<DeviceResponseDto> RegisterAsync(RegisterDeviceDto dto)
    {
        var token = dto.PushToken?.Trim() ?? string.Empty;

        if (!ExpoPushTokenRegex.IsMatch(token))
        {
            throw new ArgumentException(
                "El token de notificaciones no es válido.");
        }

        var platform = dto.Platform?.Trim().ToLowerInvariant() ?? string.Empty;

        if (!AllowedPlatforms.Contains(platform))
        {
            throw new ArgumentException(
                "La plataforma debe ser ios o android.");
        }

        await _devicesRepository.UpsertAsync(
            _currentUserService.UserId, token, platform);

        return new DeviceResponseDto
        {
            Success = true,
            Message = "Dispositivo registrado para notificaciones."
        };
    }

    public async Task<DeviceResponseDto> UnregisterAsync(UnregisterDeviceDto dto)
    {
        var token = dto.PushToken?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException(
                "El token de notificaciones es obligatorio.");
        }

        // Si el token no existe o es de otra cuenta, no hace nada: cerrar
        // sesión nunca debe fallar por esto.
        await _devicesRepository.DeleteAsync(_currentUserService.UserId, token);

        return new DeviceResponseDto
        {
            Success = true,
            Message = "Dispositivo eliminado."
        };
    }
}