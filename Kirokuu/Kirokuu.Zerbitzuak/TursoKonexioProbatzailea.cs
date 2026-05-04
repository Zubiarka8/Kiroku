using Libsql.Client;

namespace Kirokuu.Zerbitzuak;

/// <summary>
/// Verifies Turso / libSQL credentials with a trivial remote query.
/// </summary>
public static class TursoKonexioProbatzailea
{
    /// <summary>
    /// Opens a remote libSQL client using <c>TURSO_DATABASE_URL</c> and <c>TURSO_AUTH_TOKEN</c>, runs <c>SELECT 1</c>, and disposes the client.
    /// </summary>
    /// <exception cref="InvalidOperationException">When required environment variables are missing or the result is unexpected.</exception>
    public static async Task EgiaztatuSelectBatAsync(CancellationToken cancellationToken = default)
    {
        var databaseUrl = Environment.GetEnvironmentVariable("TURSO_DATABASE_URL");
        if (string.IsNullOrWhiteSpace(databaseUrl))
            throw new InvalidOperationException("TURSO_DATABASE_URL is not set.");

        var authToken = Environment.GetEnvironmentVariable("TURSO_AUTH_TOKEN");
        if (string.IsNullOrWhiteSpace(authToken))
            throw new InvalidOperationException("TURSO_AUTH_TOKEN is not set.");

        var normalizedUrl = NormalizatuTursoHttpsUrl(databaseUrl.Trim());

        using var client = await DatabaseClient.Create(options =>
        {
            options.Url = normalizedUrl;
            options.AuthToken = authToken.Trim();
            options.UseHttps = true;
        }).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        var result = await client.Execute("SELECT 1").ConfigureAwait(false);

        var firstRow = result.Rows.FirstOrDefault();
        if (firstRow is null)
            throw new InvalidOperationException("SELECT 1 returned no rows.");

        var firstValue = firstRow.FirstOrDefault();
        if (firstValue is null)
            throw new InvalidOperationException("SELECT 1 returned an empty row.");

        var text = firstValue.ToString();
        if (text != "1")
            throw new InvalidOperationException($"SELECT 1 expected value 1, got: {text}");
    }

    /// <summary>
    /// Libsql.Client expects an <c>https://</c> host URL; Turso dashboard often shows <c>libsql://</c>.
    /// </summary>
    private static string NormalizatuTursoHttpsUrl(string databaseUrl)
    {
        const string libsqlPrefix = "libsql://";
        if (databaseUrl.StartsWith(libsqlPrefix, StringComparison.OrdinalIgnoreCase))
            return "https://" + databaseUrl[libsqlPrefix.Length..];

        return databaseUrl;
    }
}
