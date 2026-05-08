namespace Kirokuu.Zerbitzuak;

/// <summary>
/// Egiaztatzen du Tursoaren autentifikazioa eta helbidea kontsulta sinple batekin (HTTPS pipeline).
/// </summary>
public static class TursoKonexioProbatzailea
{
    /// <summary>
    /// <c>TURSO_DATABASE_URL</c> eta <c>TURSO_AUTH_TOKEN</c> erabiliz <c>SELECT 1</c> exekutatzen du.
    /// </summary>
    /// <exception cref="InvalidOperationException">Aldagaiak falta dira edo erantzuna ez da espero dena.</exception>
    public static async Task EgiaztatuSelectBatAsync(CancellationToken cancellationToken = default)
    {
        var databaseUrl = Environment.GetEnvironmentVariable("TURSO_DATABASE_URL");
        if (string.IsNullOrWhiteSpace(databaseUrl))
            throw new InvalidOperationException("TURSO_DATABASE_URL is not set.");

        var authToken = Environment.GetEnvironmentVariable("TURSO_AUTH_TOKEN");
        if (string.IsNullOrWhiteSpace(authToken))
            throw new InvalidOperationException("TURSO_AUTH_TOKEN is not set.");

        var normalizedUrl = TursoHttpsHelbideaNormalizatu(databaseUrl.Trim());

        using var egikaritzaile = new TursoHttpsPipelineEgikaritzailea(normalizedUrl, authToken.Trim());
        cancellationToken.ThrowIfCancellationRequested();

        var emaitza = await egikaritzaile.ExekutatuAsync("SELECT 1", cancellationToken).ConfigureAwait(false);

        var lehenLerroa = emaitza.LerroTestuBalioak.FirstOrDefault();
        if (lehenLerroa is null || lehenLerroa.Count == 0)
            throw new InvalidOperationException("SELECT 1 returned no rows.");

        var testua = lehenLerroa[0];
        if (testua != "1")
            throw new InvalidOperationException($"SELECT 1 expected value 1, got: {testua}");
    }

    /// <summary>
    /// Turso panelak askotan <c>libsql://</c> erakusten du; HTTPS pipeline-k <c>https://</c> behar du.
    /// </summary>
    public static string TursoHttpsHelbideaNormalizatu(string databaseUrl)
    {
        const string libsqlPrefix = "libsql://";
        if (databaseUrl.StartsWith(libsqlPrefix, StringComparison.OrdinalIgnoreCase))
            return "https://" + databaseUrl[libsqlPrefix.Length..];

        return databaseUrl;
    }
}
