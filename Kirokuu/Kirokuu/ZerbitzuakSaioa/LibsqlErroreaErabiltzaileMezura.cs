using System.Net.Http;

namespace Kirokuu.ZerbitzuakSaioa;

public static class LibsqlErroreaErabiltzaileMezura
{
    public static string ZutabeEskemaMezua =>
        "Datu-basearen erantzuna ez da espero zena (zutabe edo egitura okerra). Turso eskema egiaztatu.";

    public static string BalioFormatuMezua =>
        "Datu-basearen balio bat ezin zen irakurri. Saiatu berriro edo administratzailearekin jarri harremanetan.";

    private const string DatuBaseOrokorra =
        "Datu-base errorea: ezin izan da eragiketa burutu. Saiatu berriro.";

    public static string? ErabiltzaileMezua(Exception ex)
    {
        var (nagusia, _) = ErabiltzaileMezuaXehetasunarekin(ex);
        return nagusia;
    }

    /// <summary>
    /// Erabiltzailearentzako mezua eta, datu-base eskema-errore batean, taula/zutabe xehetasuna.
    /// </summary>
    public static (string? Nagusia, string? Xehetasuna) ErabiltzaileMezuaXehetasunarekin(Exception? ex)
    {
        if (ex is null)
            return (null, null);

        foreach (var gertatu in BildatuSalbuespenZerrenda(ex))
        {
            var testua = gertatu.Message;
            if (string.IsNullOrWhiteSpace(testua))
                continue;

            var (n, x) = SaiatuMezuTeknikoaBikotea(testua);
            if (n is not null)
                return (n, x);
        }

        return (null, null);
    }

    private static IEnumerable<Exception> BildatuSalbuespenZerrenda(Exception hasiera)
    {
        var ilara = new Queue<Exception>();
        var ikusita = new HashSet<Exception>();
        ilara.Enqueue(hasiera);

        while (ilara.Count > 0)
        {
            var unekoa = ilara.Dequeue();
            if (!ikusita.Add(unekoa))
                continue;

            yield return unekoa;

            if (unekoa.InnerException is not null)
                ilara.Enqueue(unekoa.InnerException);

            if (unekoa is AggregateException agregatua)
            {
                foreach (var barnekoa in agregatua.InnerExceptions)
                    ilara.Enqueue(barnekoa);
            }
        }
    }

    public static string? AgregatuarenBarnekoErabiltzaileMezua(AggregateException aggEx)
    {
        if (aggEx is null)
            return null;

        foreach (var barnekoa in aggEx.Flatten().InnerExceptions)
        {
            var libsqlMezua = ErabiltzaileMezua(barnekoa);
            if (libsqlMezua is not null)
                return libsqlMezua;

            if (barnekoa is HttpRequestException)
                return "Sare errorea: konexioa egiaztatu eta saiatu berriro.";

            if (barnekoa is TaskCanceledException)
                return "Eskaerak denbora muga gainditu du. Saiatu berriro.";

            if (barnekoa is KeyNotFoundException)
                return ZutabeEskemaMezua;

            if (barnekoa is FormatException)
                return BalioFormatuMezua;

            if (barnekoa is UnauthorizedAccessException)
                return "Baimena ukatu da. Ezarpenetan baimena eman.";

            if (barnekoa is IOException)
                return "Fitxategi errorea: ezin izan da gordetze-operazioa burutu.";
        }

        return null;
    }

    private static string BilduTestuaKatea(Exception hasierakoa)
    {
        var zatiak = new List<string>();
        for (Exception? c = hasierakoa; c is not null; c = c.InnerException)
        {
            if (!string.IsNullOrWhiteSpace(c.Message))
                zatiak.Add(c.Message);
        }

        return string.Join(' ', zatiak);
    }

    private static string? SaiatuMezuTeknikoaMapatu(string? testua) =>
        SaiatuMezuTeknikoaBikotea(testua).Nagusia;

    private static (string? Nagusia, string? Xehetasuna) SaiatuMezuTeknikoaBikotea(string? testua)
    {
        if (string.IsNullOrWhiteSpace(testua))
            return (null, null);

        var t = testua;

        if (t.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("constraint", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("SQLITE_CONSTRAINT", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("2067", StringComparison.Ordinal))
            return ("Datu bikoiztua: sarrera hau dagoeneko existitzen da.", null);

        if (t.Contains("SQLITE_BUSY", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("database is locked", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("locked", StringComparison.OrdinalIgnoreCase))
            return ("Datu-basea okupatuta dago une honetan. Saiatu berriro pixka bat geroago.", null);

        if (t.Contains("401", StringComparison.Ordinal) ||
            t.Contains("403", StringComparison.Ordinal) ||
            t.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("forbidden", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("JWT", StringComparison.Ordinal) ||
            t.Contains("auth", StringComparison.OrdinalIgnoreCase))
            return ("Autentifikazio errorea: egiaztatu TURSO_AUTH_TOKEN eta TURSO_DATABASE_URL (.env).", null);

        if (t.Contains("no such row", StringComparison.OrdinalIgnoreCase))
            return ("Erregistroa ez da aurkitu datu-basean.", null);

        var (eskemaNagusia, eskemaXehetasuna) = DatuBaseaErroreaErabiltzaileMezura.EskuratuEskemaMezuak(t);
        if (eskemaNagusia is not null)
            return (eskemaNagusia, eskemaXehetasuna);

        return (null, null);
    }

    private static string MapatuLibsqlTestua(string agerpena) =>
        SaiatuMezuTeknikoaMapatu(agerpena) ?? DatuBaseOrokorra;
}
