using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace Kirokuu.Zerbitzuak;

/// <summary>
/// Turso SQL over HTTP: <c>POST .../v2/pipeline</c>. Ez dabil natiborik (Android/iOS/MacCatalyst); libsql/csharp_bindings ordez.
/// </summary>
public sealed class TursoHttpsPipelineEgikaritzailea : ITursoSqlEgikaritzailea, IDisposable
{
    private static readonly JsonSerializerOptions JsonAukerak = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly HttpClient _http;
    private readonly Uri _pipelineUri;
    private readonly string _token;
    private readonly ILogger? _logatzaile;
    private bool _askatuta;

    public TursoHttpsPipelineEgikaritzailea(string helbideaHttps, string token, ILogger? logatzaile = null)
    {
        if (string.IsNullOrWhiteSpace(helbideaHttps))
            throw new ArgumentException("Helbidea hutsik dago.", nameof(helbideaHttps));
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Tokena hutsik dago.", nameof(token));

        _token = token.Trim();
        _logatzaile = logatzaile;
        var oinarria = helbideaHttps.Trim().TrimEnd('/');
        if (!oinarria.EndsWith("/v2/pipeline", StringComparison.OrdinalIgnoreCase))
        {
            var baseUri = new Uri(oinarria + "/", UriKind.Absolute);
            _pipelineUri = new Uri(baseUri, "v2/pipeline");
        }
        else
        {
            _pipelineUri = new Uri(oinarria, UriKind.Absolute);
        }

        _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60)
        };
    }

    public async Task<TursoHttpExekuzioarenEmaitza> ExekutatuAsync(
        string sql,
        CancellationToken cancellationToken,
        params object?[] argumentuak)
    {
        ObjectDisposedException.ThrowIf(_askatuta, this);
        cancellationToken.ThrowIfCancellationRequested();

        var gorputza = SortuPipelineGorputza(sql, argumentuak);
        using var eskaera = new HttpRequestMessage(HttpMethod.Post, _pipelineUri)
        {
            Content = new StringContent(gorputza, Encoding.UTF8, "application/json")
        };
        eskaera.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

        using var erantzuna = await _http.SendAsync(eskaera, cancellationToken).ConfigureAwait(false);
        var testua = await erantzuna.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!erantzuna.IsSuccessStatusCode)
        {
            _logatzaile?.LogWarning(
                "Turso HTTPS pipeline HTTP {Estado}: {Ale}", (int)erantzuna.StatusCode,
                HarrapatuLaburrena(testua, 480));
            throw new TursoExekuzioSalbuespena(
                $"Turso HTTPS pipeline errorea ({(int)erantzuna.StatusCode}): {HarrapatuLaburrena(testua, 800)}.");
        }

        return ParsatuPipelineErantzuna(testua);
    }

    public void Dispose()
    {
        if (_askatuta)
            return;
        _askatuta = true;
        _http.Dispose();
    }

    private string SortuPipelineGorputza(string sql, object?[] argumentuak)
    {
        var eskaerak = new JsonArray
        {
            new JsonObject { ["type"] = "execute", ["stmt"] = SortuStmtNodoa(sql, argumentuak) },
            new JsonObject { ["type"] = "close" }
        };
        var jatorria = new JsonObject { ["requests"] = eskaerak };
        return jatorria.ToJsonString(JsonAukerak);
    }

    private static JsonObject SortuStmtNodoa(string sql, object?[] argumentuak)
    {
        var stm = new JsonObject { ["sql"] = sql };
        if (argumentuak.Length > 0)
        {
            var argsMat = new JsonArray();
            foreach (var ba in argumentuak)
                argsMat.Add(BalioraTursoArguObjetura(ba));
            stm["args"] = argsMat;
        }

        return stm;
    }

    private static JsonObject BalioraTursoArguObjetura(object? balioa)
    {
        if (balioa is null)
            return new JsonObject { ["type"] = "null" };

        if (balioa is Enum ebalio)
        {
            var zenb = Convert.ToInt32(ebalio, CultureInfo.InvariantCulture);
            return new JsonObject
            {
                ["type"] = "integer",
                ["value"] = zenb.ToString(CultureInfo.InvariantCulture)
            };
        }

        switch (balioa)
        {
            case int i:
                return new JsonObject
                {
                    ["type"] = "integer",
                    ["value"] = i.ToString(CultureInfo.InvariantCulture)
                };
            case long l:
                return new JsonObject
                {
                    ["type"] = "integer",
                    ["value"] = l.ToString(CultureInfo.InvariantCulture)
                };
            case short z:
                return new JsonObject
                {
                    ["type"] = "integer",
                    ["value"] = z.ToString(CultureInfo.InvariantCulture)
                };
            case byte b:
                return new JsonObject
                {
                    ["type"] = "integer",
                    ["value"] = b.ToString(CultureInfo.InvariantCulture)
                };
            case uint ui:
                return new JsonObject
                {
                    ["type"] = "integer",
                    ["value"] = ui.ToString(CultureInfo.InvariantCulture)
                };
            case ulong ul:
                return new JsonObject
                {
                    ["type"] = "integer",
                    ["value"] = ul.ToString(CultureInfo.InvariantCulture)
                };
            case float f:
                return new JsonObject
                {
                    ["type"] = "float",
                    ["value"] = f.ToString(CultureInfo.InvariantCulture)
                };
            case double dou:
                return new JsonObject
                {
                    ["type"] = "float",
                    ["value"] = dou.ToString(CultureInfo.InvariantCulture)
                };
            case decimal dek:
                return new JsonObject
                {
                    ["type"] = "float",
                    ["value"] = ((double)dek).ToString(CultureInfo.InvariantCulture)
                };
            case bool bo:
                return new JsonObject
                {
                    ["type"] = "integer",
                    ["value"] = (bo ? 1 : 0).ToString(CultureInfo.InvariantCulture)
                };
            case byte[] Bytes:
                return new JsonObject
                {
                    ["type"] = "blob",
                    ["base64"] = Convert.ToBase64String(Bytes)
                };
            case DateTime dto:
                return new JsonObject
                {
                    ["type"] = "text",
                    ["value"] = dto.ToString("o", CultureInfo.InvariantCulture)
                };
            default:
                return new JsonObject
                {
                    ["type"] = "text",
                    ["value"] = balioa.ToString() ?? string.Empty
                };
        }
    }

    private TursoHttpExekuzioarenEmaitza ParsatuPipelineErantzuna(string json)
    {
        var erroreaLaburra = TryGetTursoAkatsMezua(json);
        if (erroreaLaburra is not null)
            throw new TursoExekuzioSalbuespena(erroreaLaburra);

        var nodo = JsonNode.Parse(json);
        var emaitzak = nodo?["results"] as JsonArray
            ?? throw new TursoExekuzioSalbuespena("Turso erantzunan 'results' falta da.");

        foreach (var sarrera in emaitzak)
        {
            if (sarrera is null)
                continue;

            var mota = sarrera["type"]?.GetValue<string>();
            if (string.Equals(mota, "error", StringComparison.OrdinalIgnoreCase))
            {
                var akats = sarrera["error"]?.ToJsonString(JsonAukerak) ?? sarrera.ToJsonString(JsonAukerak);
                throw new TursoExekuzioSalbuespena($"Turso pipeline errorea: {akats}");
            }

            if (!string.Equals(mota, "ok", StringComparison.OrdinalIgnoreCase))
                continue;

            var erantzuna = sarrera["response"];
            if (!string.Equals(
                    erantzuna?["type"]?.GetValue<string>(),
                    "execute",
                    StringComparison.OrdinalIgnoreCase))
                continue;

            var emaitza = erantzuna?["result"];
            if (emaitza is null)
                throw new TursoExekuzioSalbuespena("Turso 'execute' erantzunan 'result' falta da.");

            return SortuTursoHttpErantzunan(emaitza);
        }

        throw new TursoExekuzioSalbuespena("Ez da Turso pipeline exekuzioren emaitzarik aurkitu.");
    }

    private TursoHttpExekuzioarenEmaitza SortuTursoHttpErantzunan(JsonNode emaitza)
    {
        var zutabeIzenak = new List<string>();
        if (emaitza["cols"] is JsonArray zutabeNodoak)
        {
            foreach (var z in zutabeNodoak)
                zutabeIzenak.Add(z?["name"]?.GetValue<string>() ?? string.Empty);
        }

        var lerrok = new List<IReadOnlyList<string>>();
        if (emaitza["rows"] is JsonArray errenkadak)
        {
            foreach (var errenkada in errenkadak)
            {
                if (errenkada is not JsonArray gelaxkakJson)
                    continue;
                var balioLista = new List<string>();
                foreach (var gx in gelaxkakJson)
                    balioLista.Add(AteraTursoGelaxkaBalioarenTestua(gx));
                lerrok.Add(balioLista);
            }
        }

        var azkenId = JasotakoOsoaInklinatuta(emaitza["last_insert_rowid"]);
        var eragindakoak = JasotakoOsoaInklinatuta(emaitza["affected_row_count"]);

        return new TursoHttpExekuzioarenEmaitza(zutabeIzenak, lerrok, azkenId, eragindakoak);
    }

    private static string AteraTursoGelaxkaBalioarenTestua(JsonNode? gelaxka)
    {
        if (gelaxka is null)
            return string.Empty;

        var mota = gelaxka["type"]?.GetValue<string>() ?? string.Empty;

        if (string.Equals(mota, "null", StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        if (string.Equals(mota, "blob", StringComparison.OrdinalIgnoreCase))
            return gelaxka["base64"]?.GetValue<string>() ?? string.Empty;

        return gelaxka["value"]?.GetValue<string>() ?? string.Empty;
    }

    private static long JasotakoOsoaInklinatuta(JsonNode? nodo)
    {
        if (nodo is null)
            return 0;

        try
        {
            switch (nodo)
            {
                case JsonValue v when v.TryGetValue<long>(out var l):
                    return l;
                case JsonValue v when v.TryGetValue<int>(out var zi):
                    return zi;
                case JsonValue v when v.TryGetValue<double>(out var d):
                    return (long)d;
                default:
                {
                    var eta = nodo.ToString()?.Trim('"') ?? string.Empty;
                    return long.TryParse(eta, NumberStyles.Integer, CultureInfo.InvariantCulture, out var x)
                        ? x
                        : 0;
                }
            }
        }
        catch
        {
            return 0;
        }
    }

    private static string? TryGetTursoAkatsMezua(string json)
    {
        try
        {
            var nodo = JsonNode.Parse(json);
            var mezua = nodo?["error"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(mezua))
                return mezua;
        }
        catch
        {
            // Ignoratu; bestela eskuz irakurriko da.
        }

        return null;
    }

    private static string HarrapatuLaburrena(string textua, int gehienez)
    {
        if (string.IsNullOrEmpty(textua))
            return string.Empty;

        var garbia = textua.ReplaceLineEndings(" ").Trim();
        return garbia.Length <= gehienez ? garbia : garbia[..gehienez] + "...";
    }
}
