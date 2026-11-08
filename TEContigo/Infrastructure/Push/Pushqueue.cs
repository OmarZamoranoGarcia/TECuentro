using System.Threading.Channels;

namespace TEContigo.Infrastructure.Push;

/// <summary>
/// Un aviso pendiente de enviar. Se guarda el usuario, no los tokens:
/// los tokens se consultan al momento de enviar.
/// </summary>
public record PushRequest(
    long UserId,
    string Title,
    string Body,
    IReadOnlyDictionary<string, object> Data);

public interface IPushQueue
{
    /// <summary>
    /// No bloquea ni lanza: encolar nunca debe frenar la petición.
    /// </summary>
    void Enqueue(PushRequest request);

    ChannelReader<PushRequest> Reader { get; }
}

public class PushQueue : IPushQueue
{
    // Acotada: si Expo estuviera caído mucho tiempo, la memoria no crece
    // sin límite; se descartan los avisos más viejos. La notificación
    // dentro de la app (tabla Notifications) ya quedó guardada igual.
    private readonly Channel<PushRequest> _channel = Channel.CreateBounded<PushRequest>(
        new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });

    public void Enqueue(PushRequest request)
    {
        _channel.Writer.TryWrite(request);
    }

    public ChannelReader<PushRequest> Reader => _channel.Reader;
}