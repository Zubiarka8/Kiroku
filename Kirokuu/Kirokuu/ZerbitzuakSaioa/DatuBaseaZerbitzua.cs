using System.Globalization;
using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.DatuEreduak;
using Kirokuu.Zerbitzuak;
using Libsql.Client;
using Microsoft.Extensions.Logging;
using SQLite;

namespace Kirokuu.ZerbitzuakSaioa;

public sealed class DatuBaseaZerbitzua
{
    private const string GarapenAdminPosta = "admin@garapena.eus";

    private readonly bool _urrunTursoModua;
    private readonly SQLiteAsyncConnection? _sqliteKonexioa;
    private readonly string? _tursoHttpsUrl;
    private readonly string? _tursoAuthToken;
    private readonly SemaphoreSlim _tursoLanSarraila = new(1, 1);
    private IDatabaseClient? _tursoBezeroa;

    private readonly PasahitzaZerbitzua _pasahitzaZerbitzua;
    private readonly ILogger<DatuBaseaZerbitzua> _logger;
    private readonly object _hasieratzeSarraila = new();
    private Task? _hasieratzeZeregina;

    public DatuBaseaZerbitzua(PasahitzaZerbitzua pasahitzaZerbitzua, ILogger<DatuBaseaZerbitzua> logger)
    {
        _pasahitzaZerbitzua = pasahitzaZerbitzua ?? throw new ArgumentNullException(nameof(pasahitzaZerbitzua));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var tursoUrl = Environment.GetEnvironmentVariable("TURSO_DATABASE_URL")?.Trim();
        var tursoToken = Environment.GetEnvironmentVariable("TURSO_AUTH_TOKEN")?.Trim();
        if (!string.IsNullOrWhiteSpace(tursoUrl) && !string.IsNullOrWhiteSpace(tursoToken))
        {
            _urrunTursoModua = true;
            _tursoHttpsUrl = TursoKonexioProbatzailea.TursoHttpsHelbideaNormalizatu(tursoUrl);
            _tursoAuthToken = tursoToken;
            _logger.LogInformation("Datu-basea: Turso/libSQL modua (TURSO_DATABASE_URL + TURSO_AUTH_TOKEN).");
        }
        else
        {
            _urrunTursoModua = false;
            var bidea = Path.Combine(FileSystem.AppDataDirectory, "kiroku_lokala.db3");
            _sqliteKonexioa = new SQLiteAsyncConnection(bidea);
            _logger.LogInformation("Datu-basea: SQLite lokala ({Bidea}).", bidea);
        }

#if DEBUG
        if (!_urrunTursoModua)
        {
            _logger.LogWarning(
                "Turso aldagaiak (TURSO_DATABASE_URL / TURSO_AUTH_TOKEN) osorik ez daude ingurunean edo .env ez da kargatu: SQLite lokala erabiliko da. Android/iOS-en APK bitartez Turso erabiltzeko, aldagaiak beste moduz (AndroidEnvironment, pipeline) eman behar dira.");
        }
#endif

        #region agent log
        var envGurasoDb = InguruneKargatzailea.BilatuEnvFitxategiarenBidea() is { } eb
            ? Path.GetDirectoryName(eb)
            : null;
        InguruneKargatzailea.ErantsiAgenteDebugNeurria(envGurasoDb, "D", "DatuBaseaZerbitzua.cs:ctor", "zerbitzua_sortzean", new Dictionary<string, object?>
        {
            ["urrunTursoModua"] = _urrunTursoModua,
            ["tursoUrlDago"] = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TURSO_DATABASE_URL")),
            ["tursoTokenDago"] = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TURSO_AUTH_TOKEN"))
        });
        #endregion
    }

    public Task HasieratuAsync()
    {
        lock (_hasieratzeSarraila)
        {
            _hasieratzeZeregina ??= HasieratuBarneanAsync();
        }

        return _hasieratzeZeregina;
    }

    public async Task<Erabiltzailea> TxertatuErabiltzaileaAsync(Erabiltzailea erabiltzailea, CancellationToken cancellationToken = default)
    {
        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (_urrunTursoModua)
            return await TxertatuTursoAsync(erabiltzailea, cancellationToken).ConfigureAwait(false);

        await _sqliteKonexioa!.InsertAsync(erabiltzailea).ConfigureAwait(false);
        var berrizIrakurria = (await _sqliteKonexioa
            .QueryAsync<Erabiltzailea>(
                "SELECT * FROM Erabiltzaileak WHERE Email = ? LIMIT 1",
                erabiltzailea.Posta)
            .ConfigureAwait(false)).FirstOrDefault();

        return berrizIrakurria ?? erabiltzailea;
    }

    public async Task<Erabiltzailea?> BilatuErabiltzaileaPostazAsync(string postaNormalizatua, CancellationToken cancellationToken = default)
    {
        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (_urrunTursoModua)
            return await BilatuErabiltzaileaPostazTursoAsync(postaNormalizatua, cancellationToken).ConfigureAwait(false);

        var zerrenda = await _sqliteKonexioa!.QueryAsync<Erabiltzailea>(
            "SELECT * FROM Erabiltzaileak WHERE Email = ? LIMIT 1",
            postaNormalizatua).ConfigureAwait(false);

        return zerrenda.FirstOrDefault();
    }

    public async Task EguneratuErabiltzaileaAsync(Erabiltzailea erabiltzailea, CancellationToken cancellationToken = default)
    {
        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (_urrunTursoModua)
        {
            await EguneratuErabiltzaileaTursoAsync(erabiltzailea, cancellationToken).ConfigureAwait(false);
            return;
        }

        await _sqliteKonexioa!.UpdateAsync(erabiltzailea).ConfigureAwait(false);
    }

    public async Task<ErabiltzaileLaburpena?> BilatuErabiltzaileLaburpenaIdzAsync(int id, CancellationToken cancellationToken = default)
    {
        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (_urrunTursoModua)
            return await BilatuErabiltzaileLaburpenaIdzTursoAsync(id, cancellationToken).ConfigureAwait(false);

        var zerrenda = await _sqliteKonexioa!.QueryAsync<ErabiltzaileLaburpena>(
            "SELECT ErabiltzaileId AS Id, Izena, Abizena, Email AS Posta FROM Erabiltzaileak WHERE ErabiltzaileId = ? LIMIT 1",
            id).ConfigureAwait(false);

        return zerrenda.FirstOrDefault();
    }

    public async Task<IReadOnlyList<ErabiltzaileLaburpena>> ZerrendatuLangileLaburpenakAsync(CancellationToken cancellationToken = default)
    {
        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (_urrunTursoModua)
            return await ZerrendatuLangileLaburpenakTursoAsync(cancellationToken).ConfigureAwait(false);

        var zerrenda = await _sqliteKonexioa!.QueryAsync<ErabiltzaileLaburpena>(
            "SELECT ErabiltzaileId AS Id, Izena, Abizena, Email AS Posta FROM Erabiltzaileak WHERE Rola = ? ORDER BY Izena COLLATE NOCASE, Abizena COLLATE NOCASE",
            (int)ErabiltzaileRola.Langilea).ConfigureAwait(false);

        return zerrenda;
    }

    private async Task HasieratuBarneanAsync()
    {
        if (_urrunTursoModua)
        {
            await ExekutatuTursoanAsync(async bezeroa =>
            {
                await BermatErabiltzaileaTaulaTursoAsync(bezeroa).ConfigureAwait(false);
#if DEBUG
                await AdministratzaileLehenarenSeedTursoAsync(bezeroa).ConfigureAwait(false);
#endif
                return 0;
            }, CancellationToken.None).ConfigureAwait(false);
            return;
        }

        await _sqliteKonexioa!.CreateTableAsync<Erabiltzailea>().ConfigureAwait(false);
        await _sqliteKonexioa.CreateTableAsync<GastuKontzeptua>().ConfigureAwait(false);
        await _sqliteKonexioa.CreateTableAsync<BidaiaTxostena>().ConfigureAwait(false);
        await _sqliteKonexioa.CreateTableAsync<GastuLerroa>().ConfigureAwait(false);
        await _sqliteKonexioa.CreateTableAsync<AuditoretzaLoga>().ConfigureAwait(false);
#if DEBUG
        await AdministratzaileLehenarenSeedGarapeneanAsync().ConfigureAwait(false);
#endif
    }

    private async Task<T> ExekutatuTursoanAsync<T>(Func<IDatabaseClient, Task<T>> lan, CancellationToken cancellationToken)
    {
        await _tursoLanSarraila.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _tursoBezeroa ??= await DatabaseClient.Create(options =>
            {
                options.Url = _tursoHttpsUrl!;
                options.AuthToken = _tursoAuthToken!;
                options.UseHttps = true;
            }).ConfigureAwait(false);

            return await lan(_tursoBezeroa).ConfigureAwait(false);
        }
        finally
        {
            _tursoLanSarraila.Release();
        }
    }

    private static async Task BermatErabiltzaileaTaulaTursoAsync(IDatabaseClient bezeroa)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS Erabiltzaileak (
                ErabiltzaileId INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                Izena TEXT NOT NULL,
                Abizena TEXT NOT NULL,
                Abizena2 TEXT NOT NULL DEFAULT '',
                DNI TEXT NOT NULL DEFAULT '',
                Email TEXT NOT NULL UNIQUE,
                Kargoa TEXT NOT NULL DEFAULT '',
                Rola INTEGER NOT NULL,
                SorkuntzaData TEXT NOT NULL DEFAULT '',
                PasahitzaHash TEXT NOT NULL,
                PasahitzaGatza TEXT NOT NULL,
                HutsuneakSaioan INTEGER NOT NULL
            );
            """;
        await bezeroa.Execute(sql).ConfigureAwait(false);
    }

    private async Task<Erabiltzailea> TxertatuTursoAsync(Erabiltzailea erabiltzailea, CancellationToken cancellationToken)
    {
        try
        {
            return await ExekutatuTursoanAsync(async bezeroa =>
            {
                const string sql = """
                    INSERT INTO Erabiltzaileak (Izena, Abizena, Abizena2, DNI, Email, Kargoa, Rola, SorkuntzaData, PasahitzaHash, PasahitzaGatza, HutsuneakSaioan)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);
                    """;
                var emaitza = await bezeroa.Execute(
                    sql,
                    erabiltzailea.Izena,
                    erabiltzailea.Abizena,
                    erabiltzailea.Abizena2,
                    erabiltzailea.Nan,
                    erabiltzailea.Posta,
                    erabiltzailea.Kargoa,
                    erabiltzailea.Rola,
                    erabiltzailea.SorkuntzaData,
                    erabiltzailea.PasahitzaHash,
                    erabiltzailea.PasahitzaGatza,
                    erabiltzailea.HutsuneakSaioan).ConfigureAwait(false);

                var idBerria = (int)emaitza.LastInsertRowId;
                erabiltzailea.Id = idBerria;
                var berrizIrakurria = await BilatuErabiltzaileaIdzTursoAsync(bezeroa, idBerria, cancellationToken).ConfigureAwait(false);
                return berrizIrakurria ?? erabiltzailea;
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (MurrizketaBikoiztuaDa(ex))
        {
            throw new ErabiltzaileMurrizketaSalbuespena("Posta bikoiztua edo murrizketa.", ex);
        }
    }

    private static bool MurrizketaBikoiztuaDa(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            var m = current.Message;
            if (m.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
                m.Contains("constraint", StringComparison.OrdinalIgnoreCase) ||
                m.Contains("2067", StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private async Task<Erabiltzailea?> BilatuErabiltzaileaPostazTursoAsync(string postaNormalizatua, CancellationToken cancellationToken)
    {
        return await ExekutatuTursoanAsync(
            bezeroa => BilatuErabiltzaileaPostazTursoBarneanAsync(bezeroa, postaNormalizatua, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Erabiltzailea?> BilatuErabiltzaileaPostazTursoBarneanAsync(
        IDatabaseClient bezeroa,
        string postaNormalizatua,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = "SELECT ErabiltzaileId, Izena, Abizena, Abizena2, DNI, Email, Kargoa, Rola, SorkuntzaData, PasahitzaHash, PasahitzaGatza, HutsuneakSaioan FROM Erabiltzaileak WHERE Email = ? LIMIT 1;";
        var emaitza = await bezeroa.Execute(sql, postaNormalizatua).ConfigureAwait(false);
        return MapeatuErabiltzaileLehena(emaitza);
    }

    private static async Task<Erabiltzailea?> BilatuErabiltzaileaIdzTursoAsync(
        IDatabaseClient bezeroa,
        int id,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = "SELECT ErabiltzaileId, Izena, Abizena, Abizena2, DNI, Email, Kargoa, Rola, SorkuntzaData, PasahitzaHash, PasahitzaGatza, HutsuneakSaioan FROM Erabiltzaileak WHERE ErabiltzaileId = ? LIMIT 1;";
        var emaitza = await bezeroa.Execute(sql, id).ConfigureAwait(false);
        return MapeatuErabiltzaileLehena(emaitza);
    }

    private async Task EguneratuErabiltzaileaTursoAsync(Erabiltzailea erabiltzailea, CancellationToken cancellationToken)
    {
        await ExekutatuTursoanAsync(async bezeroa =>
        {
            const string sql = """
                UPDATE Erabiltzaileak
                SET Izena = ?, Abizena = ?, Abizena2 = ?, DNI = ?, Email = ?, Kargoa = ?, Rola = ?, SorkuntzaData = ?, PasahitzaHash = ?, PasahitzaGatza = ?, HutsuneakSaioan = ?
                WHERE ErabiltzaileId = ?;
                """;
            await bezeroa.Execute(
                sql,
                erabiltzailea.Izena,
                erabiltzailea.Abizena,
                erabiltzailea.Abizena2,
                erabiltzailea.Nan,
                erabiltzailea.Posta,
                erabiltzailea.Kargoa,
                erabiltzailea.Rola,
                erabiltzailea.SorkuntzaData,
                erabiltzailea.PasahitzaHash,
                erabiltzailea.PasahitzaGatza,
                erabiltzailea.HutsuneakSaioan,
                erabiltzailea.Id).ConfigureAwait(false);
            return 0;
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ErabiltzaileLaburpena?> BilatuErabiltzaileLaburpenaIdzTursoAsync(int id, CancellationToken cancellationToken)
    {
        return await ExekutatuTursoanAsync(async bezeroa =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            const string sql = "SELECT ErabiltzaileId AS Id, Izena, Abizena, Email AS Posta FROM Erabiltzaileak WHERE ErabiltzaileId = ? LIMIT 1;";
            var emaitza = await bezeroa.Execute(sql, id).ConfigureAwait(false);
            return MapeatuLangileLaburpenak(emaitza).FirstOrDefault();
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<ErabiltzaileLaburpena>> ZerrendatuLangileLaburpenakTursoAsync(CancellationToken cancellationToken)
    {
        return await ExekutatuTursoanAsync(async bezeroa =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            const string sql = """
                SELECT ErabiltzaileId AS Id, Izena, Abizena, Email AS Posta FROM Erabiltzaileak
                WHERE Rola = ?
                ORDER BY Izena COLLATE NOCASE, Abizena COLLATE NOCASE;
                """;
            var emaitza = await bezeroa.Execute(sql, (int)ErabiltzaileRola.Langilea).ConfigureAwait(false);
            return MapeatuLangileLaburpenak(emaitza);
        }, cancellationToken).ConfigureAwait(false);
    }

    private static Erabiltzailea? MapeatuErabiltzaileLehena(IResultSet emaitza)
    {
        var lerroa = emaitza.Rows.FirstOrDefault();
        if (lerroa is null)
            return null;

        var zutabeak = emaitza.Columns.ToList();
        var balioak = lerroa.ToList();
        return MapeatuErabiltzailea(zutabeak, balioak);
    }

    private static string IrakurriMapaTestua(Dictionary<string, string> mapa, string gakoa)
    {
        if (!mapa.TryGetValue(gakoa, out var balioa))
            throw new KeyNotFoundException(gakoa);

        return balioa;
    }

    private static int IrakurriMapaOsoa(Dictionary<string, string> mapa, string gakoa) =>
        int.Parse(IrakurriMapaTestua(mapa, gakoa), CultureInfo.InvariantCulture);

    private static int IrakurriErabiltzaileGakoa(Dictionary<string, string> mapa, params string[] gakoak)
    {
        foreach (var gakoa in gakoak)
        {
            if (mapa.TryGetValue(gakoa, out var testua) &&
                int.TryParse(testua, NumberStyles.Integer, CultureInfo.InvariantCulture, out var balioa))
                return balioa;
        }

        throw new KeyNotFoundException(string.Join('/', gakoak));
    }

    private static string IrakurriPostaEdoEmail(Dictionary<string, string> mapa)
    {
        if (mapa.TryGetValue("Email", out var email) && !string.IsNullOrEmpty(email))
            return email;
        if (mapa.TryGetValue("Posta", out var posta))
            return posta;
        return string.Empty;
    }

    private static string IrakurriMapaTestuaLehenetsia(Dictionary<string, string> mapa, string gakoa, string lehenetsia = "") =>
        mapa.TryGetValue(gakoa, out var balioa) ? balioa : lehenetsia;

    private static int IrakurriMapaOsoaLehenetsia(Dictionary<string, string> mapa, string gakoa, int lehenetsia = 0) =>
        mapa.TryGetValue(gakoa, out var testua) &&
        int.TryParse(testua, NumberStyles.Integer, CultureInfo.InvariantCulture, out var balioa)
            ? balioa
            : lehenetsia;

    private static Erabiltzailea MapeatuErabiltzailea(IReadOnlyList<string> zutabeak, IReadOnlyList<Value> balioak)
    {
        var mapa = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < zutabeak.Count && i < balioak.Count; i++)
            mapa[zutabeak[i]] = balioak[i].ToString() ?? string.Empty;

        return new Erabiltzailea
        {
            Id = IrakurriErabiltzaileGakoa(mapa, "ErabiltzaileId", "Id"),
            Izena = IrakurriMapaTestua(mapa, "Izena"),
            Abizena = IrakurriMapaTestua(mapa, "Abizena"),
            Abizena2 = IrakurriMapaTestuaLehenetsia(mapa, "Abizena2"),
            Nan = IrakurriMapaTestuaLehenetsia(mapa, "DNI"),
            Posta = IrakurriPostaEdoEmail(mapa),
            Kargoa = IrakurriMapaTestuaLehenetsia(mapa, "Kargoa"),
            Rola = IrakurriMapaOsoa(mapa, "Rola"),
            SorkuntzaData = IrakurriMapaDataOrduaLehenetsia(mapa, "SorkuntzaData"),
            PasahitzaHash = IrakurriMapaTestuaLehenetsia(mapa, "PasahitzaHash"),
            PasahitzaGatza = IrakurriMapaTestuaLehenetsia(mapa, "PasahitzaGatza"),
            HutsuneakSaioan = IrakurriMapaOsoaLehenetsia(mapa, "HutsuneakSaioan"),
        };
    }

    private static DateTime IrakurriMapaDataOrduaLehenetsia(Dictionary<string, string> mapa, string gakoa)
    {
        if (!mapa.TryGetValue(gakoa, out var testua) || string.IsNullOrWhiteSpace(testua))
            return DateTime.UtcNow;

        if (DateTime.TryParse(
                testua,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces,
                out var dataOrdua))
            return dataOrdua;

        return DateTime.UtcNow;
    }

    private static IReadOnlyList<ErabiltzaileLaburpena> MapeatuLangileLaburpenak(IResultSet emaitza)
    {
        var zutabeak = emaitza.Columns.ToList();
        var zerrenda = new List<ErabiltzaileLaburpena>();
        foreach (var lerroa in emaitza.Rows)
        {
            var balioak = lerroa.ToList();
            var mapa = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < zutabeak.Count && i < balioak.Count; i++)
                mapa[zutabeak[i]] = balioak[i].ToString() ?? string.Empty;

            zerrenda.Add(new ErabiltzaileLaburpena
            {
                Id = IrakurriMapaOsoa(mapa, "Id"),
                Izena = IrakurriMapaTestua(mapa, "Izena"),
                Abizena = IrakurriMapaTestua(mapa, "Abizena"),
                Posta = IrakurriMapaTestua(mapa, "Posta")
            });
        }

        return zerrenda;
    }

#if DEBUG
    private async Task AdministratzaileLehenarenSeedTursoAsync(IDatabaseClient bezeroa)
    {
        try
        {
            var kopuruaEmaitza = await bezeroa.Execute(
                "SELECT COUNT(*) AS c FROM Erabiltzaileak WHERE Rola = ?;",
                (int)ErabiltzaileRola.Administratzailea).ConfigureAwait(false);

            var kopurua = IrakurriKontagailuLehena(kopuruaEmaitza);
            if (kopurua > 0)
                return;

            var (gatza, hash) = _pasahitzaZerbitzua.SortuGatzaEtaHash("Garapena123!");
            var orain = DateTime.UtcNow;
            await bezeroa.Execute(
                """
                INSERT INTO Erabiltzaileak (Izena, Abizena, Abizena2, DNI, Email, Kargoa, Rola, SorkuntzaData, PasahitzaHash, PasahitzaGatza, HutsuneakSaioan)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);
                """,
                "Admin",
                "Garapena",
                string.Empty,
                string.Empty,
                GarapenAdminPosta,
                string.Empty,
                (int)ErabiltzaileRola.Administratzailea,
                orain,
                hash,
                gatza,
                0).ConfigureAwait(false);

            _logger.LogWarning(
                "Garapeneko administratzailea sortu da (Turso): {Posta}. Pasahitza aldatu produkzioa baino lehen.",
                GarapenAdminPosta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Administratzaile seed (Turso): errorea.");
        }
    }

    private static int IrakurriKontagailuLehena(IResultSet emaitza)
    {
        var lerroa = emaitza.Rows.FirstOrDefault();
        if (lerroa is null)
            return 0;

        var balioZerrenda = lerroa.ToList();
        if (balioZerrenda.Count == 0)
            return 0;

        var testua = balioZerrenda[0].ToString() ?? "0";
        return int.Parse(testua, CultureInfo.InvariantCulture);
    }
#endif

#if DEBUG
    private async Task AdministratzaileLehenarenSeedGarapeneanAsync()
    {
        try
        {
            var kopurua = await _sqliteKonexioa!.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Erabiltzaileak WHERE Rola = ?",
                (int)ErabiltzaileRola.Administratzailea).ConfigureAwait(false);

            if (kopurua > 0)
                return;

            var (gatza, hash) = _pasahitzaZerbitzua.SortuGatzaEtaHash("Garapena123!");
            var orain = DateTime.UtcNow;
            var admin = new Erabiltzailea
            {
                Izena = "Admin",
                Abizena = "Garapena",
                Abizena2 = string.Empty,
                Nan = string.Empty,
                Posta = GarapenAdminPosta,
                Kargoa = string.Empty,
                SorkuntzaData = orain,
                PasahitzaGatza = gatza,
                PasahitzaHash = hash,
                Rola = (int)ErabiltzaileRola.Administratzailea,
                HutsuneakSaioan = 0
            };

            await _sqliteKonexioa.InsertAsync(admin).ConfigureAwait(false);
            _logger.LogWarning(
                "Garapeneko administratzailea sortu da: {Posta}. Pasahitza aldatu produkzioa baino lehen.",
                GarapenAdminPosta);
        }
        catch (SQLiteException sqlEx)
        {
            _logger.LogError(sqlEx, "Administratzaile seed: SQLite errorea.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Administratzaile seed: ustekabeko errorea.");
        }
    }
#endif
}
