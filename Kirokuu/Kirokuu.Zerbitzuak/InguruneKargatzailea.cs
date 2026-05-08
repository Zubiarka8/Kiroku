using System.Text;
using System.Text.Json;

namespace Kirokuu.Zerbitzuak;

/// <summary>
/// Locates the repo <c>.env</c> by walking parent directories and loads it via DotNetEnv.
/// </summary>
public static class InguruneKargatzailea
{
    #region agent log
    private const string AgenteIngestUrl = "http://127.0.0.1:7305/ingest/a95cf666-c357-4821-a30a-fe3da3dc054c";
    private static readonly HttpClient AgenteDebugHttp = new() { Timeout = TimeSpan.FromSeconds(2) };

    /// <summary>
    /// Garapeneko agentearentzako NDJSON erregistroa (ez dira sekretuak idazten).
    /// </summary>
    public static void ErantsiAgenteDebugNeurria(string? envGurasoBidea, string hypothesisId, string location, string message, Dictionary<string, object?>? data = null)
    {
        try
        {
            var payload = new Dictionary<string, object?>
            {
                ["sessionId"] = "d4d7d2",
                ["hypothesisId"] = hypothesisId,
                ["location"] = location,
                ["message"] = message,
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                ["data"] = data
            };
            var json = JsonSerializer.Serialize(payload);
            foreach (var logBidea in SortatuAgenteDebugBideak(envGurasoBidea))
            {
                try
                {
                    File.AppendAllText(logBidea, json + Environment.NewLine);
                    break;
                }
                catch
                {
                    // Beste bidea saiatu
                }
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    using var eskaera = new HttpRequestMessage(HttpMethod.Post, AgenteIngestUrl);
                    eskaera.Headers.TryAddWithoutValidation("X-Debug-Session-Id", "d4d7d2");
                    eskaera.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    await AgenteDebugHttp.SendAsync(eskaera).ConfigureAwait(false);
                }
                catch
                {
                    // Sare ingest aukerakoa da
                }
            });
        }
        catch
        {
            // Ez blokeatu abioa
        }
    }

    private static IEnumerable<string> SortatuAgenteDebugBideak(string? envGurasoBidea)
    {
        if (!string.IsNullOrEmpty(envGurasoBidea))
            yield return Path.Combine(envGurasoBidea, "debug-d4d7d2.log");
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kiroku", "debug-d4d7d2.log");
        yield return @"c:\Users\ZUBIA\Downloads\git\Kiroku\debug-d4d7d2.log";
    }
    #endregion

    /// <summary>
    /// Turso konexiorako ingurune aldagai nagusiak daudeen egiaztapena (balioak ez dira irakurtzen hemen).
    /// </summary>
    public static bool TursoAldagaiNagusiakDaude() =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TURSO_DATABASE_URL"))
        && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TURSO_AUTH_TOKEN"));

    /// <summary>
    /// Walks from <paramref name="startDirectory"/> (or <see cref="AppContext.BaseDirectory"/>) toward the filesystem root
    /// and returns the first path where a file named <c>.env</c> exists.
    /// </summary>
    public static string? BilatuEnvFitxategiarenBidea(string? startDirectory = null)
    {
        var current = new DirectoryInfo(startDirectory ?? AppContext.BaseDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, ".env");
            if (File.Exists(candidate))
                return candidate;

            current = current.Parent;
        }

        return null;
    }

    /// <summary>
    /// Loads environment variables from the first <c>.env</c> found upward from the start directory.
    /// </summary>
    /// <returns><c>true</c> if a file was loaded; otherwise <c>false</c>.</returns>
    public static bool KargatuDotEnv(string? startDirectory = null)
    {
        #region agent log
        var hasierakoBidea = startDirectory ?? AppContext.BaseDirectory;
        ErantsiAgenteDebugNeurria(null, "C", "InguruneKargatzailea.cs:KargatuDotEnv:sarrera", "bilaketa_hasiera", new Dictionary<string, object?>
        {
            ["hasierakoBideaAzkenZatia"] = new DirectoryInfo(hasierakoBidea).Name,
            ["currentDirAzkenZatia"] = new DirectoryInfo(Directory.GetCurrentDirectory()).Name,
            ["startDirectoryParametroa"] = startDirectory is null
        });
        #endregion

        var path = BilatuEnvFitxategiarenBidea(startDirectory);
        if (path is null && startDirectory is null)
            path = BilatuEnvFitxategiarenBidea(Directory.GetCurrentDirectory());

        var envGuraso = path is null ? null : Path.GetDirectoryName(path);

        if (path is null)
        {
            #region agent log
            ErantsiAgenteDebugNeurria(null, "A", "InguruneKargatzailea.cs:KargatuDotEnv", "env_fitxategia_ez_da_aurkitu", null);
            #endregion
            return false;
        }

        DotNetEnv.Env.Load(path, new DotNetEnv.LoadOptions(setEnvVars: true, clobberExistingVars: true, onlyExactPath: false));

        #region agent log
        ErantsiAgenteDebugNeurria(envGuraso, "B", "InguruneKargatzailea.cs:KargatuDotEnv:karga_ondoren", "dotnet_env_kargatuta", new Dictionary<string, object?>
        {
            ["envGurasoKarpeta"] = envGuraso is null ? null : new DirectoryInfo(envGuraso).Name,
            ["tursoUrlDago"] = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TURSO_DATABASE_URL")),
            ["tursoTokenDago"] = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TURSO_AUTH_TOKEN")),
            ["cloudinaryIzenaDago"] = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME"))
        });
        #endregion

        return true;
    }

    /// <summary>
    /// Android-en paketean sartutako <c>turso_garapena.env</c> bezalako fluxu batetik aldagaiak kargatzen ditu.
    /// </summary>
    public static bool KargatuDotEnvFluxutik(Stream fluxua)
    {
        if (fluxua is null)
            throw new ArgumentNullException(nameof(fluxua));

        try
        {
            DotNetEnv.Env.Load(
                fluxua,
                new DotNetEnv.LoadOptions(setEnvVars: true, clobberExistingVars: true, onlyExactPath: false));
            return true;
        }
        catch
        {
            return false;
        }
    }
}
