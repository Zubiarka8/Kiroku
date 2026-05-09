using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Kirokuu.ZerbitzuakSaioa;

/// <summary>
/// Langileak txosten berria sortzean kanpoko webhook-era jakinarazpen eskaera bidaltzen du (FCM zerbitzarian).
/// Ingurunea: KIROKU_TXOSTEN_JAKINARAZPEN_WEBHOOK_URL eta KIROKU_TXOSTEN_JAKINARAZPEN_WEBHOOK_GAKOA (.env).
/// </summary>
public sealed class TxostenBerriarenJakinarazpenBidaltzailea
{
    private const string WebhookUrlIngurunea = "KIROKU_TXOSTEN_JAKINARAZPEN_WEBHOOK_URL";

    private const string WebhookGakoaIngurunea = "KIROKU_TXOSTEN_JAKINARAZPEN_WEBHOOK_GAKOA";

    private readonly HttpClient _httpClientea;
    private readonly ILogger<TxostenBerriarenJakinarazpenBidaltzailea> _logger;

    public TxostenBerriarenJakinarazpenBidaltzailea(
        HttpClient httpClientea,
        ILogger<TxostenBerriarenJakinarazpenBidaltzailea> logger)
    {
        _httpClientea = httpClientea ?? throw new ArgumentNullException(nameof(httpClientea));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientea.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Webhook URL konfiguratuta badago, jakinarazpena bidaltzen du; bestela ez du ezer egiten.
    /// </summary>
    public async Task SaiatuBidaliTxostenBerriaSortuDelaAsync(
        int langileErabiltzaileId,
        string langileIzenOsoa,
        string helmuga,
        string deskribapena,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var url = Environment.GetEnvironmentVariable(WebhookUrlIngurunea)?.Trim();
        var gakoa = Environment.GetEnvironmentVariable(WebhookGakoaIngurunea)?.Trim();
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(gakoa))
            return;

        var gorputza = new TxostenBerriarenJakinarazpenWebhookGorputza
        {
            Ekintza = "txosten_berria",
            WebhookGakoa = gakoa,
            LangileErabiltzaileId = langileErabiltzaileId,
            LangileIzenOsoa = langileIzenOsoa,
            Helmuga = helmuga,
            Deskribapena = deskribapena
        };

        try
        {
            using var erantzuna = await _httpClientea
                .PostAsJsonAsync(url, gorputza, cancellationToken)
                .ConfigureAwait(false);

            if (!erantzuna.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Txosten jakinarazpen webhook: HTTP {Kodea}.",
                    (int)erantzuna.StatusCode);
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "Txosten jakinarazpen webhook: sare errorea.");
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Txosten jakinarazpen webhook: denbora muga.");
        }
        catch (InvalidOperationException opEx)
        {
            _logger.LogError(opEx, "Txosten jakinarazpen webhook: URL edo eskaera baliogabea.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Txosten jakinarazpen webhook: ustekabeko errorea.");
        }
    }

    private sealed class TxostenBerriarenJakinarazpenWebhookGorputza
    {
        [JsonPropertyName("ekintza")]
        public string Ekintza { get; set; } = string.Empty;

        [JsonPropertyName("webhookGakoa")]
        public string WebhookGakoa { get; set; } = string.Empty;

        [JsonPropertyName("langileErabiltzaileId")]
        public int LangileErabiltzaileId { get; set; }

        [JsonPropertyName("langileIzenOsoa")]
        public string LangileIzenOsoa { get; set; } = string.Empty;

        [JsonPropertyName("helmuga")]
        public string Helmuga { get; set; } = string.Empty;

        [JsonPropertyName("deskribapena")]
        public string Deskribapena { get; set; } = string.Empty;
    }
}
