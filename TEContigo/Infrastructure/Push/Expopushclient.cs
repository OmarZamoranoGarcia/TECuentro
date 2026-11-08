using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TEContigo.Infrastructure.Push;

public class ExpoPushMessage
{
    public string To { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public IReadOnlyDictionary<string, object>? Data { get; set; }
    public string Sound { get; set; } = "default";
    public string Priority { get; set; } = "high";
}

public class ExpoTicket
{
    public string Status { get; set; } = string.Empty;
    public string? Id { get; set; }
    public string? Message { get; set; }
    public ExpoTicketDetails? Details { get; set; }
}

public class ExpoTicketDetails
{
    public string? Error { get; set; }
}

public class ExpoPushClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;

    public ExpoPushClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://exp.host/");
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        // Opcional: solo si activaste "enhanced push security" en tu
        // proyecto de Expo. Sin esto funciona igual.
        var accessToken = configuration["Expo:AccessToken"];

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
        }
    }

    /// <summary>
    /// Expo acepta hasta 100 mensajes por llamada y responde un ticket por
    /// cada uno, en el mismo orden.
    /// </summary>
    public async Task<IReadOnlyList<ExpoTicket>> SendAsync(
        IReadOnlyCollection<ExpoPushMessage> messages,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "--/api/v2/push/send", messages, JsonOptions, cancellationToken);

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ExpoResponse>(
            JsonOptions, cancellationToken);

        return body?.Data ?? new List<ExpoTicket>();
    }

    private sealed class ExpoResponse
    {
        public List<ExpoTicket>? Data { get; set; }
    }
}