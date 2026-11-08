using TEContigo.Modules.Devices.Repositories;

namespace TEContigo.Infrastructure.Push;

public class PushBackgroundService : BackgroundService
{
    private const int MaxRequestsPerBatch = 50;
    private const int ExpoMaxMessagesPerCall = 100;

    private readonly IPushQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PushBackgroundService> _logger;

    public PushBackgroundService(
        IPushQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<PushBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var reader = _queue.Reader;

        try
        {
            while (await reader.WaitToReadAsync(stoppingToken))
            {
                var batch = new List<PushRequest>();

                while (batch.Count < MaxRequestsPerBatch && reader.TryRead(out var request))
                {
                    batch.Add(request);
                }

                try
                {
                    await SendBatchAsync(batch, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Un fallo de Expo o de red no debe detener la cola.
                    _logger.LogError(ex, "No se pudo enviar un lote de notificaciones push.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Cierre normal de la aplicación.
        }
    }

    private async Task SendBatchAsync(List<PushRequest> batch, CancellationToken cancellationToken)
    {
        // El servicio de fondo es singleton; el repositorio y el cliente HTTP
        // no, así que se resuelven en un scope propio por lote.
        using var scope = _scopeFactory.CreateScope();

        var devices = scope.ServiceProvider.GetRequiredService<IUserDevicesRepository>();
        var expo = scope.ServiceProvider.GetRequiredService<ExpoPushClient>();

        var messages = new List<ExpoPushMessage>();

        foreach (var request in batch)
        {
            var tokens = await devices.GetTokensByUserIdAsync(request.UserId);

            foreach (var token in tokens)
            {
                messages.Add(new ExpoPushMessage
                {
                    To = token,
                    Title = request.Title,
                    Body = request.Body,
                    Data = request.Data
                });
            }
        }

        foreach (var chunk in messages.Chunk(ExpoMaxMessagesPerCall))
        {
            var tickets = await expo.SendAsync(chunk, cancellationToken);

            var deadTokens = chunk
                .Zip(tickets, (message, ticket) => (message, ticket))
                .Where(pair => pair.ticket.Status == "error" &&
                               pair.ticket.Details?.Error == "DeviceNotRegistered")
                .Select(pair => pair.message.To)
                .ToList();

            if (deadTokens.Count > 0)
            {
                await devices.DeleteByTokensAsync(deadTokens);
            }

            foreach (var ticket in tickets.Where(t =>
                         t.Status == "error" && t.Details?.Error != "DeviceNotRegistered"))
            {
                _logger.LogWarning(
                    "Expo rechazó una notificación: {Error} - {Message}",
                    ticket.Details?.Error,
                    ticket.Message);
            }
        }
    }
}