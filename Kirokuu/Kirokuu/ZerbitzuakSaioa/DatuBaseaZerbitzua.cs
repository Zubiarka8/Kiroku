using System.Globalization;
using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.DatuEreduak;
using Kirokuu.Zerbitzuak;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using SQLite;

namespace Kirokuu.ZerbitzuakSaioa;

public sealed partial class DatuBaseaZerbitzua
{
    // Libsql.Client: UTF-8 zero byte ataletan fixed() punteroa null da; libsql_bind_string kolapsatu daiteke.
    private const string LibsqlKateHutsarenOrdezkoa = "\u2060";

    private readonly bool _urrunTursoModua;
    private SQLiteAsyncConnection? _sqliteKonexioa;
    private readonly string? _sqliteDatuBaseBidea;
    private readonly string? _tursoHttpsUrl;
    private readonly string? _tursoAuthToken;
    private readonly SemaphoreSlim _tursoLanSarraila = new(1, 1);
    private TursoHttpsPipelineEgikaritzailea? _tursoHttpsEgikaritzailea;

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
            _sqliteDatuBaseBidea = Path.Combine(FileSystem.AppDataDirectory, "kiroku_lokala.db3");
            _logger.LogInformation("Datu-basea: SQLite lokala SQLCipher-rekin ({Bidea}).", _sqliteDatuBaseBidea);
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

    public async Task<Erabiltzailea?> BilatuErabiltzaileaIdzAsync(int id, CancellationToken cancellationToken = default)
    {
        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (_urrunTursoModua)
            return await ExekutatuTursoanAsync(
                bezeroa => BilatuErabiltzaileaIdzTursoAsync(bezeroa, id, cancellationToken),
                cancellationToken).ConfigureAwait(false);

        var zerrenda = await _sqliteKonexioa!.QueryAsync<Erabiltzailea>(
            "SELECT * FROM Erabiltzaileak WHERE ErabiltzaileId = ? LIMIT 1",
            id).ConfigureAwait(false);
        return zerrenda.FirstOrDefault();
    }

    public async Task<int?> EskuratuAdministratzailearenSektoreIragazkiaAsync(
        int administratzaileErabiltzaileId,
        CancellationToken cancellationToken = default)
    {
        if (administratzaileErabiltzaileId <= 0)
            return null;

        var admin = await BilatuErabiltzaileaIdzAsync(administratzaileErabiltzaileId, cancellationToken).ConfigureAwait(false);
        var sektoreId = admin?.SektorearenIdentifikatzailea ?? 0;
        return sektoreId > 0 ? sektoreId : null;
    }

    public async Task<bool> ErabiltzaileaSektorearekinBatDatorAsync(
        int erabiltzaileId,
        int sektoreIragazkia,
        CancellationToken cancellationToken = default)
    {
        if (erabiltzaileId <= 0 || sektoreIragazkia <= 0)
            return false;

        var erabiltzailea = await BilatuErabiltzaileaIdzAsync(erabiltzaileId, cancellationToken).ConfigureAwait(false);
        return erabiltzailea?.SektorearenIdentifikatzailea == sektoreIragazkia;
    }

    public async Task<bool> NANErabilitaDagoaAsync(string nan, int? ezezErabiltzaileId = null, CancellationToken cancellationToken = default)
    {
        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var nanGarbia = nan.Trim();

        if (_urrunTursoModua)
            return await ExekutatuTursoanAsync(async bezeroa =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (ezezErabiltzaileId.HasValue)
                {
                    const string sql = "SELECT ErabiltzaileId FROM Erabiltzaileak WHERE DNI = ? AND ErabiltzaileId != ? LIMIT 1;";
                    var em = await bezeroa.ExekutatuAsync(sql, cancellationToken, LibsqlLoturaNormalizatua(nanGarbia), LibsqlLoturaNormalizatua(ezezErabiltzaileId.Value)).ConfigureAwait(false);
                    return em.LerroTestuBalioak.Count > 0;
                }
                else
                {
                    const string sql = "SELECT ErabiltzaileId FROM Erabiltzaileak WHERE DNI = ? LIMIT 1;";
                    var em = await bezeroa.ExekutatuAsync(sql, cancellationToken, LibsqlLoturaNormalizatua(nanGarbia)).ConfigureAwait(false);
                    return em.LerroTestuBalioak.Count > 0;
                }
            }, cancellationToken).ConfigureAwait(false);

        List<Erabiltzailea> zerrenda;
        if (ezezErabiltzaileId.HasValue)
            zerrenda = await _sqliteKonexioa!.QueryAsync<Erabiltzailea>(
                "SELECT ErabiltzaileId FROM Erabiltzaileak WHERE DNI = ? AND ErabiltzaileId != ? LIMIT 1",
                nanGarbia, ezezErabiltzaileId.Value).ConfigureAwait(false);
        else
            zerrenda = await _sqliteKonexioa!.QueryAsync<Erabiltzailea>(
                "SELECT ErabiltzaileId FROM Erabiltzaileak WHERE DNI = ? LIMIT 1",
                nanGarbia).ConfigureAwait(false);

        return zerrenda.Count > 0;
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
            """
            SELECT ErabiltzaileId AS Id, Izena, Abizena, Email AS Posta,
                   COALESCE(Aktiboa, 1) AS Aktiboa
            FROM Erabiltzaileak WHERE ErabiltzaileId = ? LIMIT 1
            """,
            id).ConfigureAwait(false);

        return zerrenda.FirstOrDefault();
    }

    public async Task<IReadOnlyList<ErabiltzaileLaburpena>> ZerrendatuLangileLaburpenakAsync(
        int? sektoreIragazkia = null,
        CancellationToken cancellationToken = default)
    {
        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (_urrunTursoModua)
            return await ZerrendatuLangileLaburpenakTursoAsync(sektoreIragazkia, cancellationToken).ConfigureAwait(false);

        if (sektoreIragazkia is > 0)
        {
            return await _sqliteKonexioa!.QueryAsync<ErabiltzaileLaburpena>(
                """
                SELECT ErabiltzaileId AS Id, Izena, Abizena, Email AS Posta,
                       COALESCE(Aktiboa, 1) AS Aktiboa
                FROM Erabiltzaileak
                WHERE Rola = ? AND Sektorea = ?
                ORDER BY Izena COLLATE NOCASE, Abizena COLLATE NOCASE
                """,
                (int)ErabiltzaileRola.Langilea,
                sektoreIragazkia.Value).ConfigureAwait(false);
        }

        var zerrenda = await _sqliteKonexioa!.QueryAsync<ErabiltzaileLaburpena>(
            """
            SELECT ErabiltzaileId AS Id, Izena, Abizena, Email AS Posta,
                   COALESCE(Aktiboa, 1) AS Aktiboa
            FROM Erabiltzaileak
            WHERE Rola = ?
            ORDER BY Izena COLLATE NOCASE, Abizena COLLATE NOCASE
            """,
            (int)ErabiltzaileRola.Langilea).ConfigureAwait(false);

        return zerrenda;
    }

    private async Task BermatSqliteKonexioaSortutaAsync()
    {
        if (_sqliteKonexioa != null || _urrunTursoModua)
            return;

        var bidea = _sqliteDatuBaseBidea ?? throw new InvalidOperationException("SQLite bidea ez dago konfiguratuta.");

        var flags = SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex;
        var gordetakoHex = await SecureStorage.GetAsync(DatuBaseaZifraketaLaguntzailea.GakoarenBiltegiGakoa).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(gordetakoHex))
        {
            byte[] gakoByteak;
            try
            {
                gakoByteak = Convert.FromHexString(gordetakoHex);
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "SQLCipher gakoaren hex balioa baliogabea.");
                throw new InvalidOperationException("Datu-basearen zifraketa-gakoa hondatuta dago.", ex);
            }

            if (gakoByteak.Length != 32)
                throw new InvalidOperationException("Datu-basearen zifraketa-gakoaren luzera ez da zuzena.");

            var cs = new SQLiteConnectionString(bidea, flags, storeDateTimeAsTicks: true, key: gakoByteak);
            _sqliteKonexioa = new SQLiteAsyncConnection(cs);
            await EgiaztatuSqliteKonexioaAsync().ConfigureAwait(false);
            return;
        }

        if (File.Exists(bidea))
        {
            var lauCs = new SQLiteConnectionString(bidea, flags, storeDateTimeAsTicks: true, key: null);
            _sqliteKonexioa = new SQLiteAsyncConnection(lauCs);
            await EgiaztatuSqliteKonexioaAsync().ConfigureAwait(false);

            var berria = DatuBaseaZifraketaLaguntzailea.SortuZufakoGakoByteak();
            await _sqliteKonexioa.ReKeyAsync(berria).ConfigureAwait(false);
            await SecureStorage.SetAsync(DatuBaseaZifraketaLaguntzailea.GakoarenBiltegiGakoa, Convert.ToHexString(berria)).ConfigureAwait(false);
            _logger.LogInformation("SQLite datu-base zaharra (testu laua) zifratu da.");
            return;
        }

        var sortutakoGakoa = DatuBaseaZifraketaLaguntzailea.SortuZufakoGakoByteak();
        await SecureStorage.SetAsync(DatuBaseaZifraketaLaguntzailea.GakoarenBiltegiGakoa, Convert.ToHexString(sortutakoGakoa)).ConfigureAwait(false);

        var zifratuCs = new SQLiteConnectionString(bidea, flags, storeDateTimeAsTicks: true, key: sortutakoGakoa);
        _sqliteKonexioa = new SQLiteAsyncConnection(zifratuCs);
        await EgiaztatuSqliteKonexioaAsync().ConfigureAwait(false);
    }

    private async Task EgiaztatuSqliteKonexioaAsync()
    {
        await _sqliteKonexioa!.ExecuteScalarAsync<int>("SELECT 1").ConfigureAwait(false);
        await _sqliteKonexioa.ExecuteAsync("PRAGMA foreign_keys = ON").ConfigureAwait(false);
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
                    await BermatErabiltzaileaTaulaTursoAsync(bezeroa, CancellationToken.None).ConfigureAwait(false);
                    await BermatuTursoGainerakoTaulakEtaZutabeakAsync(bezeroa, CancellationToken.None).ConfigureAwait(false);
#if DEBUG
                    await AdministratzaileLehenarenSeedTursoAsync(bezeroa, CancellationToken.None).ConfigureAwait(false);
                    await AdministratzaileProbakoDatuakTursoAsync(bezeroa, CancellationToken.None).ConfigureAwait(false);
#endif
                    return 0;
                }, CancellationToken.None).ConfigureAwait(false);
                return;
            }

            await BermatSqliteKonexioaSortutaAsync().ConfigureAwait(false);

            await SortuSqliteTaulaAsync<Erabiltzailea>("Erabiltzaileak").ConfigureAwait(false);
            await SortuSqliteTaulaAsync<GastuKontzeptua>("GastuKontzeptuak").ConfigureAwait(false);
            await SeedSqliteGastuKontzeptuakAsync().ConfigureAwait(false);
            await SortuSqliteBidaiaTxostenaFKrekinAsync().ConfigureAwait(false);
            await SortuSqliteGastuLerroaFKrekinAsync().ConfigureAwait(false);
            await SortuSqliteAuditoretzaLogaFKrekinAsync().ConfigureAwait(false);
            await MigraAuditoretzaLogaDiruSarreraIdTxostenIdraSqliteAsync().ConfigureAwait(false);
            await MigraAuditoretzaLogaLangileIdGehituSqliteAsync().ConfigureAwait(false);
#if DEBUG
                    await AdministratzaileLehenarenSeedGarapeneanAsync().ConfigureAwait(false);
                    await AdministratzaileProbakoDatuakSQLiteAsync().ConfigureAwait(false);
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

    private async Task SeedSqliteGastuKontzeptuakAsync()
    {
        var kopurua = await _sqliteKonexioa!.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM GastuKontzeptuak").ConfigureAwait(false);
        if (kopurua >= 9)
            return;

        var kontzeptuak = new GastuKontzeptua[]
        {
            new() { KategoriaId = 1, Izena = "Bazkaria",         Deskribapena = "Jangela eta bazkari gastuak",   IbilgailuaBeharDu = 0, Estatusa = "Aktibo", GastuKontzeptuId = 1 },
            new() { KategoriaId = 2, Izena = "Gasolina",         Deskribapena = "Erregai gastuak",               IbilgailuaBeharDu = 1, Estatusa = "Aktibo", GastuKontzeptuId = 2 },
            new() { KategoriaId = 3, Izena = "Garraio publikoa", Deskribapena = "Autobus, metro eta trena",      IbilgailuaBeharDu = 0, Estatusa = "Aktibo", GastuKontzeptuId = 3 },
            new() { KategoriaId = 4, Izena = "Hotela",           Deskribapena = "Ostatua eta gau-pasak",         IbilgailuaBeharDu = 0, Estatusa = "Aktibo", GastuKontzeptuId = 4 },
            new() { KategoriaId = 5, Izena = "Peajea",           Deskribapena = "Autobide eta tunelak",          IbilgailuaBeharDu = 1, Estatusa = "Aktibo", GastuKontzeptuId = 5 },
            new() { KategoriaId = 6, Izena = "Aparkalekua",      Deskribapena = "Aparkagune gastuak",            IbilgailuaBeharDu = 1, Estatusa = "Aktibo", GastuKontzeptuId = 6 },
            new() { KategoriaId = 7, Izena = "Bidaia",           Deskribapena = "Hegazkin eta garraio nagusiak", IbilgailuaBeharDu = 0, Estatusa = "Aktibo", GastuKontzeptuId = 7 },
            new() { KategoriaId = 8, Izena = "Materialak",       Deskribapena = "Bulego eta lan materialak",     IbilgailuaBeharDu = 0, Estatusa = "Aktibo", GastuKontzeptuId = 8 },
            new() { KategoriaId = 9, Izena = "Bestelakoa",       Deskribapena = "Sailkatu gabeko gastuak",       IbilgailuaBeharDu = 0, Estatusa = "Aktibo", GastuKontzeptuId = 9 }
        };

        foreach (var k in kontzeptuak)
            await _sqliteKonexioa.InsertOrReplaceAsync(k).ConfigureAwait(false);
    }

    private async Task SortuSqliteTaulaAsync<T>(string taulaIzena) where T : new()
    {
        GarapenLogaDebug($"SQLite taula sortzen edo egiaztatzen: {taulaIzena}.");
        await _sqliteKonexioa!.CreateTableAsync<T>().ConfigureAwait(false);
        GarapenLogaDebug($"SQLite taula prest: {taulaIzena}.");
    }

    private async Task<T> ExekutatuTursoanAsync<T>(Func<ITursoSqlEgikaritzailea, Task<T>> lan, CancellationToken cancellationToken)
    {
        await _tursoLanSarraila.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _tursoHttpsEgikaritzailea ??= new TursoHttpsPipelineEgikaritzailea(_tursoHttpsUrl!, _tursoAuthToken!, _logger);

            return await lan(_tursoHttpsEgikaritzailea).ConfigureAwait(false);
        }
        finally
        {
            _tursoLanSarraila.Release();
        }
    }

    private static async Task BermatErabiltzaileaTaulaTursoAsync(ITursoSqlEgikaritzailea bezeroa, CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS Erabiltzaileak (
                ErabiltzaileId INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                Izena TEXT NOT NULL,
                Abizena TEXT NOT NULL,
                Abizena2 TEXT NOT NULL DEFAULT '',
                DNI TEXT NOT NULL UNIQUE DEFAULT '',
                Email TEXT NOT NULL UNIQUE,
                Kargoa TEXT NOT NULL DEFAULT '',
                Sektorea TEXT NOT NULL DEFAULT '',
                Rola INTEGER NOT NULL,
                SorkuntzaData TEXT NOT NULL DEFAULT '',
                Pasahitza TEXT NOT NULL DEFAULT ''
            );
            """;
        await bezeroa.ExekutatuAsync(sql, cancellationToken).ConfigureAwait(false);
    }

    private async Task SortuSqliteBidaiaTxostenaFKrekinAsync()
    {
        await _sqliteKonexioa!.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS BidaiaTxostenak (
                TxostenId INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                ErabiltzaileId INTEGER NOT NULL,
                LangileDNI TEXT NOT NULL DEFAULT '',
                Saila TEXT NOT NULL DEFAULT '',
                Helmuga TEXT NOT NULL DEFAULT '',
                BidaiaHelburua TEXT NOT NULL DEFAULT '',
                HasieraData TEXT NOT NULL DEFAULT '',
                AmaieraData TEXT NOT NULL DEFAULT '',
                PertsonaKopurua INTEGER NOT NULL DEFAULT 0,
                JasoAurrerakina INTEGER NOT NULL DEFAULT 0,
                Egoera TEXT NOT NULL DEFAULT '',
                AdminOharra TEXT,
                AdminDNI TEXT,
                EmpresaIbilgailua INTEGER NOT NULL DEFAULT 0,
                MonetaKodea TEXT NOT NULL DEFAULT '',
                SorkuntzaData TEXT NOT NULL DEFAULT '',
                AzkenEguneraketa TEXT NOT NULL DEFAULT '',
                DataAprobazioa TEXT NOT NULL DEFAULT '',
                FOREIGN KEY (ErabiltzaileId) REFERENCES Erabiltzaileak(ErabiltzaileId) ON DELETE RESTRICT,
                FOREIGN KEY (LangileDNI) REFERENCES Erabiltzaileak(DNI) ON DELETE RESTRICT ON UPDATE CASCADE,
                FOREIGN KEY (AdminDNI) REFERENCES Erabiltzaileak(DNI) ON DELETE RESTRICT ON UPDATE CASCADE
            );
            """).ConfigureAwait(false);
    }

    private async Task SortuSqliteGastuLerroaFKrekinAsync()
    {
        await _sqliteKonexioa!.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS GastuLerroak (
                GastuId INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                TxostenId INTEGER NOT NULL DEFAULT 0,
                KategoriaId INTEGER NOT NULL DEFAULT 0,
                GastuData TEXT NOT NULL DEFAULT '',
                GarraioBidea TEXT NOT NULL DEFAULT '',
                Zenbatekoa_Guztira REAL NOT NULL DEFAULT 0,
                Kilometroak REAL NOT NULL DEFAULT 0,
                TicketArgazkiBidea TEXT NOT NULL DEFAULT '',
                Oharrak TEXT NOT NULL DEFAULT '',
                KontzeptuId INTEGER NOT NULL DEFAULT 0,
                IbilgailuaBeharrezkoa INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (TxostenId) REFERENCES BidaiaTxostenak(TxostenId) ON DELETE CASCADE,
                FOREIGN KEY (KategoriaId) REFERENCES GastuKontzeptuak(KategoriaId) ON DELETE RESTRICT
            );
            """).ConfigureAwait(false);
    }

    private async Task SortuSqliteAuditoretzaLogaFKrekinAsync()
    {
        await _sqliteKonexioa!.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS AuditoretzaLoga (
                LogId INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                TxostenId INTEGER,
                ErabiltzaileId INTEGER NOT NULL DEFAULT 0,
                LangileId INTEGER,
                Ekintza TEXT NOT NULL DEFAULT '',
                DataOrdua TEXT NOT NULL DEFAULT '',
                Deskribapena TEXT NOT NULL DEFAULT '',
                IP_Helbidea TEXT NOT NULL DEFAULT '',
                FOREIGN KEY (ErabiltzaileId) REFERENCES Erabiltzaileak(ErabiltzaileId) ON DELETE RESTRICT,
                FOREIGN KEY (LangileId) REFERENCES Erabiltzaileak(ErabiltzaileId) ON DELETE SET NULL,
                FOREIGN KEY (TxostenId) REFERENCES BidaiaTxostenak(TxostenId) ON DELETE SET NULL
            );
            """).ConfigureAwait(false);
    }


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
                    INSERT INTO Erabiltzaileak (Izena, Abizena, Abizena2, DNI, Email, Kargoa, Sektorea, Rola, SorkuntzaData, Pasahitza, Aktiboa, SaioHasieraSaiakerak, SaioaBlokeoaAmaieraUtc)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);
                    """;
                var emaitza = await bezeroa.ExekutatuAsync(
                    sql,
                    cancellationToken,
                    LibsqlLoturaNormalizatua(erabiltzailea.Izena),
                    LibsqlLoturaNormalizatua(erabiltzailea.Abizena),
                    LibsqlLoturaNormalizatua(erabiltzailea.Abizena2),
                    LibsqlLoturaNormalizatua(erabiltzailea.DNI),
                    LibsqlLoturaNormalizatua(erabiltzailea.Posta),
                    LibsqlLoturaNormalizatua(erabiltzailea.Kargoa),
                    LibsqlLoturaNormalizatua(erabiltzailea.Sektorea),
                    LibsqlLoturaNormalizatua(erabiltzailea.Rola),
                    LibsqlLoturaNormalizatua(erabiltzailea.SorkuntzaData),
                    LibsqlLoturaNormalizatua(erabiltzailea.Pasahitza),
                    LibsqlLoturaNormalizatua(erabiltzailea.Aktiboa),
                    LibsqlLoturaNormalizatua(erabiltzailea.SaioHasieraSaiakerak),
                    LibsqlLoturaNormalizatua(erabiltzailea.SaioaBlokeoaAmaieraUtc)
                ).ConfigureAwait(false);
                var idBerria = (int)emaitza.AzkenTxertatutakoErrenkadaId;
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
        ITursoSqlEgikaritzailea bezeroa,
        string postaNormalizatua,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = "SELECT ErabiltzaileId, Izena, Abizena, Abizena2, DNI, Email, Kargoa, Sektorea, Rola, SorkuntzaData, Pasahitza, Aktiboa, COALESCE(SaioHasieraSaiakerak, 0) AS SaioHasieraSaiakerak, SaioaBlokeoaAmaieraUtc FROM Erabiltzaileak WHERE Email = ? LIMIT 1;";
        var emaitza = await bezeroa.ExekutatuAsync(sql, cancellationToken, LibsqlLoturaNormalizatua(postaNormalizatua)).ConfigureAwait(false);
        return MapeatuErabiltzaileLehena(emaitza);
    }

    private static async Task<Erabiltzailea?> BilatuErabiltzaileaIdzTursoAsync(
        ITursoSqlEgikaritzailea bezeroa,
        int id,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = "SELECT ErabiltzaileId, Izena, Abizena, Abizena2, DNI, Email, Kargoa, Sektorea, Rola, SorkuntzaData, Pasahitza, Aktiboa, COALESCE(SaioHasieraSaiakerak, 0) AS SaioHasieraSaiakerak, SaioaBlokeoaAmaieraUtc FROM Erabiltzaileak WHERE ErabiltzaileId = ? LIMIT 1;";
        var emaitza = await bezeroa.ExekutatuAsync(sql, cancellationToken, id).ConfigureAwait(false);
        return MapeatuErabiltzaileLehena(emaitza);
    }

    private async Task EguneratuErabiltzaileaTursoAsync(Erabiltzailea erabiltzailea, CancellationToken cancellationToken)
    {
        await ExekutatuTursoanAsync(async bezeroa =>
        {
            const string sql = """
                UPDATE Erabiltzaileak
                SET Izena = ?, Abizena = ?, Abizena2 = ?, DNI = ?, Email = ?, Kargoa = ?, Sektorea = ?, Rola = ?, SorkuntzaData = ?, Pasahitza = ?, Aktiboa = ?, SaioHasieraSaiakerak = ?, SaioaBlokeoaAmaieraUtc = ?
                WHERE ErabiltzaileId = ?;
                """;
            await bezeroa.ExekutatuAsync(
                sql,
                cancellationToken,
                LibsqlLoturaNormalizatua(erabiltzailea.Izena),
                LibsqlLoturaNormalizatua(erabiltzailea.Abizena),
                LibsqlLoturaNormalizatua(erabiltzailea.Abizena2),
                LibsqlLoturaNormalizatua(erabiltzailea.DNI),
                LibsqlLoturaNormalizatua(erabiltzailea.Posta),
                LibsqlLoturaNormalizatua(erabiltzailea.Kargoa),
                LibsqlLoturaNormalizatua(erabiltzailea.Sektorea),
                LibsqlLoturaNormalizatua(erabiltzailea.Rola),
                LibsqlLoturaNormalizatua(erabiltzailea.SorkuntzaData),
                LibsqlLoturaNormalizatua(erabiltzailea.Pasahitza),
                LibsqlLoturaNormalizatua(erabiltzailea.Aktiboa),
                LibsqlLoturaNormalizatua(erabiltzailea.SaioHasieraSaiakerak),
                LibsqlLoturaNormalizatua(erabiltzailea.SaioaBlokeoaAmaieraUtc),
                LibsqlLoturaNormalizatua(erabiltzailea.Id)).ConfigureAwait(false);
            return 0;
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ErabiltzaileLaburpena?> BilatuErabiltzaileLaburpenaIdzTursoAsync(int id, CancellationToken cancellationToken)
    {
        return await ExekutatuTursoanAsync(async bezeroa =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            const string sql = """
                SELECT ErabiltzaileId AS Id, Izena, Abizena, Email AS Posta,
                       COALESCE(Aktiboa, 1) AS Aktiboa
                FROM Erabiltzaileak WHERE ErabiltzaileId = ? LIMIT 1;
                """;
            var emaitza = await bezeroa.ExekutatuAsync(sql, cancellationToken, id).ConfigureAwait(false);
            return MapeatuLangileLaburpenak(emaitza).FirstOrDefault();
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<ErabiltzaileLaburpena>> ZerrendatuLangileLaburpenakTursoAsync(
        int? sektoreIragazkia,
        CancellationToken cancellationToken)
    {
        return await ExekutatuTursoanAsync(async bezeroa =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            TursoHttpExekuzioarenEmaitza emaitza;
            if (sektoreIragazkia is > 0)
            {
                const string sqlSektorea = """
                    SELECT ErabiltzaileId AS Id, Izena, Abizena, Email AS Posta,
                           COALESCE(Aktiboa, 1) AS Aktiboa
                    FROM Erabiltzaileak
                    WHERE Rola = ? AND Sektorea = ?
                    ORDER BY Izena COLLATE NOCASE, Abizena COLLATE NOCASE;
                    """;
                emaitza = await bezeroa.ExekutatuAsync(
                    sqlSektorea,
                    cancellationToken,
                    (int)ErabiltzaileRola.Langilea,
                    LibsqlLoturaNormalizatua(sektoreIragazkia.Value)).ConfigureAwait(false);
            }
            else
            {
                const string sql = """
                    SELECT ErabiltzaileId AS Id, Izena, Abizena, Email AS Posta,
                           COALESCE(Aktiboa, 1) AS Aktiboa
                    FROM Erabiltzaileak
                    WHERE Rola = ?
                    ORDER BY Izena COLLATE NOCASE, Abizena COLLATE NOCASE;
                    """;
                emaitza = await bezeroa.ExekutatuAsync(sql, cancellationToken, (int)ErabiltzaileRola.Langilea).ConfigureAwait(false);
            }

            return MapeatuLangileLaburpenak(emaitza);
        }, cancellationToken).ConfigureAwait(false);
    }

    private static Erabiltzailea? MapeatuErabiltzaileLehena(TursoHttpExekuzioarenEmaitza emaitza)
    {
        var lerroa = emaitza.LerroTestuBalioak.FirstOrDefault();
        if (lerroa is null)
            return null;

        var zutabeak = emaitza.ZutabeIzenak;
        var balioak = lerroa;
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

    private static Erabiltzailea MapeatuErabiltzailea(IReadOnlyList<string> zutabeak, IReadOnlyList<string> balioak)
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
            Sektorea = IrakurriMapaTestuaLehenetsia(mapa, "Sektorea"),
            Rola = IrakurriMapaOsoa(mapa, "Rola"),
            SorkuntzaData = DataOrduaBalioak.MapatikDataOrdua(mapa, "SorkuntzaData"),
            Pasahitza = IrakurriMapaTestuaLehenetsia(mapa, "Pasahitza"),
            Aktiboa = IrakurriMapaOsoaLehenetsia(mapa, "Aktiboa", 1),
            SaioHasieraSaiakerak = IrakurriMapaOsoaLehenetsia(mapa, "SaioHasieraSaiakerak", 0),
            SaioaBlokeoaAmaieraUtc = IrakurriMapaTestuaHutsikNull(mapa, "SaioaBlokeoaAmaieraUtc")
        };
    }

    private static string? IrakurriMapaTestuaHutsikNull(Dictionary<string, string> mapa, string gakoa)
    {
        if (!mapa.TryGetValue(gakoa, out var testua) || string.IsNullOrWhiteSpace(testua))
            return null;

        var garbia = TursoTestuaIrakurri(testua);
        return string.IsNullOrEmpty(garbia) ? null : garbia;
    }

    private static IReadOnlyList<ErabiltzaileLaburpena> MapeatuLangileLaburpenak(TursoHttpExekuzioarenEmaitza emaitza)
    {
        var zutabeak = emaitza.ZutabeIzenak;
        var zerrenda = new List<ErabiltzaileLaburpena>();
        foreach (var lerroa in emaitza.LerroTestuBalioak)
        {
            var balioak = lerroa;
            var mapa = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < zutabeak.Count && i < balioak.Count; i++)
                mapa[zutabeak[i]] = TursoTestuaIrakurri(balioak[i].ToString() ?? string.Empty);

            zerrenda.Add(new ErabiltzaileLaburpena
            {
                Id = IrakurriMapaOsoa(mapa, "Id"),
                Izena = IrakurriMapaTestua(mapa, "Izena"),
                Abizena = IrakurriMapaTestua(mapa, "Abizena"),
                Posta = IrakurriMapaTestua(mapa, "Posta"),
                Aktiboa = IrakurriMapaOsoaLehenetsia(mapa, "Aktiboa", 1)
            });
        }

        return zerrenda;
    }

#if DEBUG
    private async Task AdministratzaileLehenarenSeedTursoAsync(ITursoSqlEgikaritzailea bezeroa, CancellationToken cancellationToken)
    {
        try
        {
            var kopuruaEmaitza = await bezeroa.ExekutatuAsync(
                "SELECT COUNT(*) AS c FROM Erabiltzaileak WHERE Rola = ?;",
                cancellationToken,
                (int)ErabiltzaileRola.Administratzailea).ConfigureAwait(false);

            var kopurua = IrakurriKontagailuLehena(kopuruaEmaitza);
            if (kopurua > 0)
                return;

            var (adminIzena, adminAbizena, adminAbizena2, adminDni, adminPosta, adminKargoa, adminPasahitza) =
                EskuratuGarapenAdminSeedBalioak();
            if (adminPosta is null)
                return;

            var (adminSektorea, adminKargoTestua) = EskuratuGarapenAdminSektoreaEtaKargoarenBalioak(adminKargoa);
            var (gatza, hash) = _pasahitzaZerbitzua.SortuGatzaEtaHash(adminPasahitza);
            var orain = DataOrduaBalioak.DataOrduaOrain();
            var pasahitzaKatea = _pasahitzaZerbitzua.LotuGatzaEtaHashKatean(gatza, hash);
            await bezeroa.ExekutatuAsync(
                """
                INSERT INTO Erabiltzaileak (Izena, Abizena, Abizena2, DNI, Email, Kargoa, Sektorea, Rola, SorkuntzaData, Pasahitza, Aktiboa, SaioHasieraSaiakerak, SaioaBlokeoaAmaieraUtc)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);
                """,
                cancellationToken,
                LibsqlLoturaNormalizatua(adminIzena),
                LibsqlLoturaNormalizatua(adminAbizena),
                LibsqlLoturaNormalizatua(adminAbizena2),
                LibsqlLoturaNormalizatua(adminDni),
                LibsqlLoturaNormalizatua(adminPosta),
                LibsqlLoturaNormalizatua(adminKargoTestua),
                LibsqlLoturaNormalizatua(adminSektorea),
                LibsqlLoturaNormalizatua((int)ErabiltzaileRola.Administratzailea),
                LibsqlLoturaNormalizatua(orain),
                LibsqlLoturaNormalizatua(pasahitzaKatea),
                LibsqlLoturaNormalizatua(1),
                LibsqlLoturaNormalizatua(0),
                LibsqlLoturaNormalizatua(null)).ConfigureAwait(false);

            _logger.LogWarning(
                "Garapeneko administratzailea sortu da (Turso): {Posta}. Pasahitza aldatu produkzioa baino lehen.",
                adminPosta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Administratzaile seed (Turso): errorea.");
        }
    }

    private static int IrakurriKontagailuLehena(TursoHttpExekuzioarenEmaitza emaitza)
    {
        var lerroa = emaitza.LerroTestuBalioak.FirstOrDefault();
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

            var (adminSektorea, adminKargoTestua) = EskuratuGarapenAdminSektoreaEtaKargoarenBalioak(adminKargoa);
            var (gatza, hash) = _pasahitzaZerbitzua.SortuGatzaEtaHash(adminPasahitza);
            var orain = DataOrduaBalioak.DataOrduaOrain();
            var admin = new Erabiltzailea
            {
                Izena = adminIzena,
                Abizena = adminAbizena,
                Abizena2 = adminAbizena2,
                DNI = adminDni,
                Posta = adminPosta,
                Kargoa = adminKargoTestua,
                SektorearenIdentifikatzailea = adminSektorea,
                SorkuntzaData = orain,
                Pasahitza = _pasahitzaZerbitzua.LotuGatzaEtaHashKatean(gatza, hash),
                Rola = (int)ErabiltzaileRola.Administratzailea,
                Aktiboa = 1
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
    // Aukerako: KIROKU_GARAPEN_ADMIN_ABIZENA2, KIROKU_GARAPEN_ADMIN_DNI, KIROKU_GARAPEN_ADMIN_KARGOA,
    // KIROKU_GARAPEN_ADMIN_SEKTOREA (1–3), KIROKU_GARAPEN_ADMIN_KARGOA_ID (EnpresakoLangileKargoa zenbakia).
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

    private static (int Sektorea, string KargoarenTestua) EskuratuGarapenAdminSektoreaEtaKargoarenBalioak(string kargoarenTestuZaharra)
    {
        var sektorLehenetsia = AdministratzaileOrganizazioLehenetsia.SektorearenIdentifikatzailea;
        var kargoLehenetsia = (int)EnpresakoLangileKargoa.AdministratzaileSistema;
        var sektorEnv = Environment.GetEnvironmentVariable("KIROKU_GARAPEN_ADMIN_SEKTOREA")?.Trim();
        var kargoIdEnv = Environment.GetEnvironmentVariable("KIROKU_GARAPEN_ADMIN_KARGOA_ID")?.Trim();
        var sektor = int.TryParse(sektorEnv, NumberStyles.Integer, CultureInfo.InvariantCulture, out var s) ? s : sektorLehenetsia;
        var kargoId = int.TryParse(kargoIdEnv, NumberStyles.Integer, CultureInfo.InvariantCulture, out var k) ? k : kargoLehenetsia;

        if (!SektoreaKargoarenHiztegia.KargoakSektorearekinBatDator(sektor, kargoId))
        {
            sektor = sektorLehenetsia;
            kargoId = kargoLehenetsia;
        }

        var testua = string.IsNullOrWhiteSpace(kargoarenTestuZaharra)
            ? SektoreaKargoarenHiztegia.LortuKargoarenEtiketa((EnpresakoLangileKargoa)kargoId)
            : kargoarenTestuZaharra.Trim();

        if (string.IsNullOrWhiteSpace(testua))
            testua = SektoreaKargoarenHiztegia.LortuKargoarenEtiketa((EnpresakoLangileKargoa)kargoId);

        return (sektor, testua);
    }
#endif

}
