namespace TEContigo.Modules.Devices.Repositories;

public interface IUserDevicesRepository
{
    /// <summary>
    /// Inserta el token, o si ya existía lo reasigna al usuario actual
    /// (por ejemplo, un celular que cambia de cuenta).
    /// </summary>
    Task UpsertAsync(long userId, string pushToken, string platform);

    /// <summary>
    /// Solo borra si el token pertenece a ese usuario.
    /// </summary>
    Task DeleteAsync(long userId, string pushToken);

    Task<IEnumerable<string>> GetTokensByUserIdAsync(long userId);

    /// <summary>
    /// Limpieza de tokens que Expo reporta como inválidos.
    /// </summary>
    Task DeleteByTokensAsync(IEnumerable<string> pushTokens);
}