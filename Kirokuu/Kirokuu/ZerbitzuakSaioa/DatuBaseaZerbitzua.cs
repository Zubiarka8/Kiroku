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
    // Libsql.Client: UTF-8 zero byte ataletan fixed() punteroa null da; libsql_bind_string kolapsatu daiteke.
    private const string LibsqlKateHutsarenOrdezkoa = "\u2060";

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

        _logger.LogDebug("Erabiltzailea txertatzen hasi da. Modua: {Modua}.", _urrunTursoModua ? "Turso" : "SQLite");
        GarapenLogaDebug($"Erabiltzailea txertatzen hasi da. Modua: {(_urrunTursoModua ? "Turso" : "SQLite")}.");

        try
        {
            if (_urrunTursoModua)
                return await TxertatuTursoAsync(erabiltzailea, cancellationToken).ConfigureAwait(false);

            await _sqliteKonexioa!.InsertAsync(erabiltzailea).ConfigureAwait(false);
            var berrizIrakurria = (await _sqliteKonexioa
                .QueryAsync<Erabiltzailea>(
                    "SELECT * FROM Erabiltzaileak WHERE Email = ? LIMIT 1",
                    erabiltzailea.Posta)
                .ConfigureAwait(false)).FirstOrDefault();

            _logger.LogDebug("Erabiltzailea ondo txertatu da SQLite datu-basean.");
            GarapenLogaDebug("Erabiltzailea ondo txertatu da SQLite datu-basean.");
            return berrizIrakurria ?? erabiltzailea;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erabiltzailea txertatzean errorea. Modua: {Modua}.", _urrunTursoModua ? "Turso" : "SQLite");
            GarapenLogaError(ex, $"Erabiltzailea txertatzean errorea. Modua: {(_urrunTursoModua ? "Turso" : "SQLite")}.");
            throw;
        }
    }

    public async Task<Erabiltzailea?> BilatuErabiltzaileaPostazAsync(string postaNormalizatua, CancellationToken cancellationToken = default)
    {
        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (_urrunTursoModua)
            return await BilatuErabiltzaileaPostazTursoAsync(postaNormalizatua, cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("Erabiltzailea postaz bilatzen hasi da SQLite datu-basean.");
        GarapenLogaDebug("Erabiltzailea postaz bilatzen hasi da SQLite datu-basean.");
        var zerrenda = await _sqliteKonexioa!.QueryAsync<Erabiltzailea>(
            "SELECT * FROM Erabiltzaileak WHERE Email = ? LIMIT 1",
            postaNormalizatua).ConfigureAwait(false);

        _logger.LogDebug("Erabiltzailea postaz bilatzea amaitu da SQLite datu-basean. Aurkitua: {Aurkitua}.", zerrenda.Count > 0);
        GarapenLogaDebug($"Erabiltzailea postaz bilatzea amaitu da SQLite datu-basean. Aurkitua: {zerrenda.Count > 0}.");
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

        _logger.LogDebug("Erabiltzailea eguneratzen hasi da SQLite datu-basean.");
        await _sqliteKonexioa!.UpdateAsync(erabiltzailea).ConfigureAwait(false);
        _logger.LogDebug("Erabiltzailea ondo eguneratu da SQLite datu-basean.");
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
        try
        {
            _logger.LogDebug("Datu-basea hasieratzen hasi da. Modua: {Modua}.", _urrunTursoModua ? "Turso" : "SQLite");
            GarapenLogaDebug($"Datu-basea hasieratzen hasi da. Modua: {(_urrunTursoModua ? "Turso" : "SQLite")}.");
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

            await SortuSqliteTaulaAsync<Erabiltzailea>("Erabiltzaileak").ConfigureAwait(false);
            await BermatuSqliteErabiltzaileEskemaAsync().ConfigureAwait(false);
            await SortuSqliteTaulaAsync<GastuKontzeptua>("GastuKontzeptuak").ConfigureAwait(false);
            await SortuSqliteTaulaAsync<BidaiaTxostena>("BidaiaTxostenak").ConfigureAwait(false);
            await SortuSqliteTaulaAsync<GastuLerroa>("GastuLerroak").ConfigureAwait(false);
            await SortuSqliteTaulaAsync<AuditoretzaLoga>("AuditoretzaLoga").ConfigureAwait(false);
#if DEBUG
            await AdministratzaileLehenarenSeedGarapeneanAsync().ConfigureAwait(false);
#endif
            _logger.LogDebug("Datu-basea hasieratzea amaitu da.");
            GarapenLogaDebug("Datu-basea hasieratzea amaitu da.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Datu-basea hasieratzean errorea.");
            GarapenLogaError(ex, "Datu-basea hasieratzean errorea.");
            throw;
        }
    }

    private async Task SortuSqliteTaulaAsync<T>(string taulaIzena) where T : new()
    {
        GarapenLogaDebug($"SQLite taula sortzen edo egiaztatzen: {taulaIzena}.");
        await _sqliteKonexioa!.CreateTableAsync<T>().ConfigureAwait(false);
        GarapenLogaDebug($"SQLite taula prest: {taulaIzena}.");
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
                Pasahitza TEXT NOT NULL DEFAULT ''
            );
            """;
        await bezeroa.Execute(sql).ConfigureAwait(false);
    }

    private async Task BermatuSqliteErabiltzaileEskemaAsync()
    {
        _logger.LogDebug("SQLite Erabiltzaileak eskema egiaztatzen hasi da.");
        GarapenLogaDebug("SQLite Erabiltzaileak eskema egiaztatzen hasi da.");
        var zutabeak = await IrakurriSqliteErabiltzaileZutabeakAsync().ConfigureAwait(false);

        if (!zutabeak.Contains("Pasahitza"))
        {
            await _sqliteKonexioa!.ExecuteAsync("ALTER TABLE Erabiltzaileak ADD COLUMN Pasahitza TEXT NOT NULL DEFAULT ''").ConfigureAwait(false);
            _logger.LogWarning("SQLite Erabiltzaileak taulan Pasahitza zutabea gehitu da.");
            GarapenLogaWarning("SQLite Erabiltzaileak taulan Pasahitza zutabea gehitu da.");
        }

        zutabeak = await IrakurriSqliteErabiltzaileZutabeakAsync().ConfigureAwait(false);
        await MigratuSqlitePasahitzZaharrakAsync(zutabeak).ConfigureAwait(false);
        await KenduSqliteNanIndizeBakarraAsync().ConfigureAwait(false);
        _logger.LogDebug("SQLite Erabiltzaileak eskema egiaztatzea amaitu da.");
        GarapenLogaDebug("SQLite Erabiltzaileak eskema egiaztatzea amaitu da.");
    }

    private async Task<HashSet<string>> IrakurriSqliteErabiltzaileZutabeakAsync()
    {
        var zutabeak = await _sqliteKonexioa!.QueryAsync<SqliteZutabea>("PRAGMA table_info('Erabiltzaileak')").ConfigureAwait(false);
        return zutabeak
            .Select(zutabea => zutabea.Izena)
            .Where(izena => !string.IsNullOrWhiteSpace(izena))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private async Task MigratuSqlitePasahitzZaharrakAsync(HashSet<string> zutabeak)
    {
        if (!zutabeak.Contains("Pasahitza"))
            return;

        var erabiltzaileak = await _sqliteKonexioa!.QueryAsync<SqlitePasahitzZaharra>(
            """
            SELECT ErabiltzaileId, Pasahitza FROM Erabiltzaileak
            WHERE Pasahitza IS NOT NULL AND Pasahitza <> ''
              AND instr(Pasahitza, '|') = 0
            """).ConfigureAwait(false);

        foreach (var erabiltzailea in erabiltzaileak)
        {
            var (gatza, hash) = _pasahitzaZerbitzua.SortuGatzaEtaHash(erabiltzailea.Pasahitza);
            var katea = _pasahitzaZerbitzua.LotuGatzaEtaHashKatean(gatza, hash);
            await _sqliteKonexioa.ExecuteAsync(
                    "UPDATE Erabiltzaileak SET Pasahitza = ? WHERE ErabiltzaileId = ?",
                    katea,
                    erabiltzailea.Id)
                .ConfigureAwait(false);
        }

        if (erabiltzaileak.Count > 0)
            _logger.LogWarning("SQLite erabiltzaile zaharren pasahitzak hash+gatzaren formatu bakarrera migratu dira. Kopurua: {Kopurua}.", erabiltzaileak.Count);
    }

    private async Task KenduSqliteNanIndizeBakarraAsync()
    {
        var indizeak = await _sqliteKonexioa!.QueryAsync<SqliteIndizea>("PRAGMA index_list('Erabiltzaileak')").ConfigureAwait(false);
        foreach (var indizea in indizeak)
        {
            if (indizea.Bakarra != 1)
                continue;

            var zutabeak = await _sqliteKonexioa.QueryAsync<SqliteIndizeZutabea>(
                $"PRAGMA index_info({KomatxoBikoitzekin(indizea.Izena)})").ConfigureAwait(false);
            var nanIndizeaDa = zutabeak.Any(zutabea =>
                string.Equals(zutabea.Izena, "DNI", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(zutabea.Izena, "Nan", StringComparison.OrdinalIgnoreCase));

            if (!nanIndizeaDa)
                continue;

            await _sqliteKonexioa.ExecuteAsync($"DROP INDEX IF EXISTS {KomatxoBikoitzekin(indizea.Izena)}").ConfigureAwait(false);
            _logger.LogWarning("SQLite Erabiltzaileak taulako DNI indize bakarra kendu da: {Indizea}.", indizea.Izena);
            GarapenLogaWarning($"SQLite Erabiltzaileak taulako DNI indize bakarra kendu da: {indizea.Izena}.");
        }
    }

    private static string KomatxoBikoitzekin(string izena) => "\"" + izena.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    private static void GarapenLogaDebug(string mezua)
    {
#if DEBUG && ANDROID
        Android.Util.Log.Debug("KirokuuDebug", mezua);
#endif
    }

    private static void GarapenLogaWarning(string mezua)
    {
#if DEBUG && ANDROID
        Android.Util.Log.Warn("KirokuuDebug", mezua);
#endif
    }

    private static void GarapenLogaError(Exception ex, string mezua)
    {
#if DEBUG && ANDROID
        Android.Util.Log.Error("KirokuuDebug", $"{mezua} {ex.GetType().Name}: {ex.Message}");
#endif
    }

    private async Task<Erabiltzailea> TxertatuTursoAsync(Erabiltzailea erabiltzailea, CancellationToken cancellationToken)
    {
        try
        {
            return await ExekutatuTursoanAsync(async bezeroa =>
            {
                const string sql = """
                    INSERT INTO Erabiltzaileak (Izena, Abizena, Abizena2, DNI, Email, Kargoa, Rola, SorkuntzaData, Pasahitza)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?);
                    """;
                var emaitza = await bezeroa.Execute(
                    sql,
                    LibsqlLoturaNormalizatua(erabiltzailea.Izena),
                    LibsqlLoturaNormalizatua(erabiltzailea.Abizena),
                    LibsqlLoturaNormalizatua(erabiltzailea.Abizena2),
                    LibsqlLoturaNormalizatua(erabiltzailea.DNI),
                    LibsqlLoturaNormalizatua(erabiltzailea.Posta),
                    LibsqlLoturaNormalizatua(erabiltzailea.Kargoa),
                    LibsqlLoturaNormalizatua(erabiltzailea.Rola),
                    LibsqlLoturaNormalizatua(erabiltzailea.SorkuntzaData),
                    LibsqlLoturaNormalizatua(erabiltzailea.Pasahitza)
                ).ConfigureAwait(false);
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
        const string sql = "SELECT ErabiltzaileId, Izena, Abizena, Abizena2, DNI, Email, Kargoa, Rola, SorkuntzaData, Pasahitza FROM Erabiltzaileak WHERE Email = ? LIMIT 1;";
        var emaitza = await bezeroa.Execute(sql, LibsqlLoturaNormalizatua(postaNormalizatua)).ConfigureAwait(false);
        return MapeatuErabiltzaileLehena(emaitza);
    }

    private static async Task<Erabiltzailea?> BilatuErabiltzaileaIdzTursoAsync(
        IDatabaseClient bezeroa,
        int id,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = "SELECT ErabiltzaileId, Izena, Abizena, Abizena2, DNI, Email, Kargoa, Rola, SorkuntzaData, Pasahitza FROM Erabiltzaileak WHERE ErabiltzaileId = ? LIMIT 1;";
        var emaitza = await bezeroa.Execute(sql, id).ConfigureAwait(false);
        return MapeatuErabiltzaileLehena(emaitza);
    }

    private async Task EguneratuErabiltzaileaTursoAsync(Erabiltzailea erabiltzailea, CancellationToken cancellationToken)
    {
        await ExekutatuTursoanAsync(async bezeroa =>
        {
            const string sql = """
                UPDATE Erabiltzaileak
                SET Izena = ?, Abizena = ?, Abizena2 = ?, DNI = ?, Email = ?, Kargoa = ?, Rola = ?, SorkuntzaData = ?, Pasahitza = ?
                WHERE ErabiltzaileId = ?;
                """;
            await bezeroa.Execute(
                sql,
                LibsqlLoturaNormalizatua(erabiltzailea.Izena),
                LibsqlLoturaNormalizatua(erabiltzailea.Abizena),
                LibsqlLoturaNormalizatua(erabiltzailea.Abizena2),
                LibsqlLoturaNormalizatua(erabiltzailea.DNI),
                LibsqlLoturaNormalizatua(erabiltzailea.Posta),
                LibsqlLoturaNormalizatua(erabiltzailea.Kargoa),
                LibsqlLoturaNormalizatua(erabiltzailea.Rola),
                LibsqlLoturaNormalizatua(erabiltzailea.SorkuntzaData),
                LibsqlLoturaNormalizatua(erabiltzailea.Pasahitza),
                LibsqlLoturaNormalizatua(erabiltzailea.Id)).ConfigureAwait(false);
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

    private static object LibsqlLoturaNormalizatua(object? balioa) =>
        balioa is string s && s.Length == 0 ? LibsqlKateHutsarenOrdezkoa : balioa!;

    private static string TursoTestuaIrakurri(string? gordeta)
    {
        if (string.IsNullOrEmpty(gordeta))
            return string.Empty;

        return gordeta == LibsqlKateHutsarenOrdezkoa ? string.Empty : gordeta;
    }

    private static string IrakurriMapaTestua(Dictionary<string, string> mapa, string gakoa)
    {
        if (!mapa.TryGetValue(gakoa, out var balioa))
            throw new KeyNotFoundException(gakoa);

        return TursoTestuaIrakurri(balioa);
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
            return TursoTestuaIrakurri(email);
        if (mapa.TryGetValue("Posta", out var posta))
            return TursoTestuaIrakurri(posta);
        return string.Empty;
    }

    private static string IrakurriMapaTestuaLehenetsia(Dictionary<string, string> mapa, string gakoa, string lehenetsia = "") =>
        mapa.TryGetValue(gakoa, out var balioa) ? TursoTestuaIrakurri(balioa) : lehenetsia;

    private static int IrakurriMapaOsoaLehenetsia(Dictionary<string, string> mapa, string gakoa, int lehenetsia = 0) =>
        mapa.TryGetValue(gakoa, out var testua) &&
        int.TryParse(testua, NumberStyles.Integer, CultureInfo.InvariantCulture, out var balioa)
            ? balioa
            : lehenetsia;

    private static Erabiltzailea MapeatuErabiltzailea(IReadOnlyList<string> zutabeak, IReadOnlyList<Value> balioak)
    {
        var mapa = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < zutabeak.Count && i < balioak.Count; i++)
            mapa[zutabeak[i]] = TursoTestuaIrakurri(balioak[i].ToString() ?? string.Empty);

        return new Erabiltzailea
        {
            Id = IrakurriErabiltzaileGakoa(mapa, "ErabiltzaileId", "Id"),
            Izena = IrakurriMapaTestua(mapa, "Izena"),
            Abizena = IrakurriMapaTestua(mapa, "Abizena"),
            Abizena2 = IrakurriMapaTestuaLehenetsia(mapa, "Abizena2"),
            DNI = IrakurriMapaTestuaLehenetsia(mapa, "DNI"),
            Posta = IrakurriPostaEdoEmail(mapa),
            Kargoa = IrakurriMapaTestuaLehenetsia(mapa, "Kargoa"),
            Rola = IrakurriMapaOsoa(mapa, "Rola"),
            SorkuntzaData = IrakurriMapaDataOrduaLehenetsia(mapa, "SorkuntzaData")
                .ToString("o", CultureInfo.InvariantCulture),
            Pasahitza = IrakurriMapaTestuaLehenetsia(mapa, "Pasahitza")
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
                mapa[zutabeak[i]] = TursoTestuaIrakurri(balioak[i].ToString() ?? string.Empty);

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

            var (adminIzena, adminAbizena, adminAbizena2, adminDni, adminPosta, adminKargoa, adminPasahitza) =
                EskuratuGarapenAdminSeedBalioak();
            if (adminPosta is null)
                return;

            var (gatza, hash) = _pasahitzaZerbitzua.SortuGatzaEtaHash(adminPasahitza);
            var orain = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            var pasahitzaKatea = _pasahitzaZerbitzua.LotuGatzaEtaHashKatean(gatza, hash);
            await bezeroa.Execute(
                """
                INSERT INTO Erabiltzaileak (Izena, Abizena, Abizena2, DNI, Email, Kargoa, Rola, SorkuntzaData, Pasahitza)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?);
                """,
                LibsqlLoturaNormalizatua(adminIzena),
                LibsqlLoturaNormalizatua(adminAbizena),
                LibsqlLoturaNormalizatua(adminAbizena2),
                LibsqlLoturaNormalizatua(adminDni),
                LibsqlLoturaNormalizatua(adminPosta),
                LibsqlLoturaNormalizatua(adminKargoa),
                LibsqlLoturaNormalizatua((int)ErabiltzaileRola.Administratzailea),
                LibsqlLoturaNormalizatua(orain),
                LibsqlLoturaNormalizatua(pasahitzaKatea)).ConfigureAwait(false);

            _logger.LogWarning(
                "Garapeneko administratzailea sortu da (Turso): {Posta}. Pasahitza aldatu produkzioa baino lehen.",
                adminPosta);
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

            var (adminIzena, adminAbizena, adminAbizena2, adminDni, adminPosta, adminKargoa, adminPasahitza) =
                EskuratuGarapenAdminSeedBalioak();
            if (adminPosta is null)
                return;

            var (gatza, hash) = _pasahitzaZerbitzua.SortuGatzaEtaHash(adminPasahitza);
            var orain = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            var admin = new Erabiltzailea
            {
                Izena = adminIzena,
                Abizena = adminAbizena,
                Abizena2 = adminAbizena2,
                DNI = adminDni,
                Posta = adminPosta,
                Kargoa = adminKargoa,
                SorkuntzaData = orain,
                Pasahitza = _pasahitzaZerbitzua.LotuGatzaEtaHashKatean(gatza, hash),
                Rola = (int)ErabiltzaileRola.Administratzailea
            };

            await _sqliteKonexioa.InsertAsync(admin).ConfigureAwait(false);
            _logger.LogWarning(
                "Garapeneko administratzailea sortu da: {Posta}. Pasahitza aldatu produkzioa baino lehen.",
                adminPosta);
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

    // DEBUG: KIROKU_GARAPEN_ADMIN_POSTA, KIROKU_GARAPEN_ADMIN_PASAHITZA, KIROKU_GARAPEN_ADMIN_IZENA, KIROKU_GARAPEN_ADMIN_ABIZENA derrigorrez.
    // Aukerako: KIROKU_GARAPEN_ADMIN_ABIZENA2, KIROKU_GARAPEN_ADMIN_DNI, KIROKU_GARAPEN_ADMIN_KARGOA.
    private (string Izena, string Abizena, string Abizena2, string Dni, string? Posta, string Kargoa, string Pasahitza)
        EskuratuGarapenAdminSeedBalioak()
    {
        var posta = Environment.GetEnvironmentVariable("KIROKU_GARAPEN_ADMIN_POSTA")?.Trim();
        var pasahitza = Environment.GetEnvironmentVariable("KIROKU_GARAPEN_ADMIN_PASAHITZA");
        var izena = Environment.GetEnvironmentVariable("KIROKU_GARAPEN_ADMIN_IZENA")?.Trim();
        var abizena = Environment.GetEnvironmentVariable("KIROKU_GARAPEN_ADMIN_ABIZENA")?.Trim();
        if (string.IsNullOrWhiteSpace(posta) || string.IsNullOrWhiteSpace(pasahitza) ||
            string.IsNullOrWhiteSpace(izena) || string.IsNullOrWhiteSpace(abizena))
        {
            _logger.LogInformation(
                "Administratzaile seed: KIROKU_GARAPEN_ADMIN_POSTA, _PASAHITZA, _IZENA eta _ABIZENA behar dira; ez da administratzailea sortuko.");
            return (string.Empty, string.Empty, string.Empty, string.Empty, null, string.Empty, string.Empty);
        }

        var abizena2 = Environment.GetEnvironmentVariable("KIROKU_GARAPEN_ADMIN_ABIZENA2")?.Trim() ?? string.Empty;
        var dni = Environment.GetEnvironmentVariable("KIROKU_GARAPEN_ADMIN_DNI")?.Trim() ?? string.Empty;
        var kargoa = Environment.GetEnvironmentVariable("KIROKU_GARAPEN_ADMIN_KARGOA")?.Trim() ?? string.Empty;
        return (izena, abizena, abizena2, dni, posta, kargoa, pasahitza);
    }
#endif

    private sealed class SqliteZutabea
    {
        [Column("name")]
        public string Izena { get; set; } = string.Empty;
    }

    private sealed class SqliteIndizea
    {
        [Column("name")]
        public string Izena { get; set; } = string.Empty;

        [Column("unique")]
        public int Bakarra { get; set; }
    }

    private sealed class SqliteIndizeZutabea
    {
        [Column("name")]
        public string Izena { get; set; } = string.Empty;
    }

    private sealed class SqlitePasahitzZaharra
    {
        [Column("ErabiltzaileId")]
        public int Id { get; set; }

        public string Pasahitza { get; set; } = string.Empty;
    }
}
