using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.DatuEreduak;
using Kirokuu.AplikazioZerbitzuak;
using Kirokuu.Zerbitzuak;
using Microsoft.Extensions.Logging;
using SQLite;

namespace Kirokuu.ZerbitzuakSaioa;

public sealed partial class DatuBaseaZerbitzua
{
    private const string AuditoretzaEkintzaTxostenaOnartu = "TxostenaOnartua";

    private const string AuditoretzaEkintzaTxostenaUkatu = "TxostenaEzeztatu";

    private const string AuditoretzaEkintzaTxostenaEskatuDu = "TxostenaEskatuDu";

    private const string AuditoretzaEkintzaTxostenaBertanBehera = "TxostenaBertanBehera";

    private async Task BermatuTursoGainerakoTaulakEtaZutabeakAsync(ITursoSqlEgikaritzailea bezeroa, CancellationToken cancellationToken)
    {
        await bezeroa.ExekutatuAsync("PRAGMA foreign_keys = ON;", cancellationToken).ConfigureAwait(false);

        var gastuKontzeptuakSql = DatuBasea.DatuBaseaSortzeAginduak.IrakurriSortzeAgindua("GastuKontzeptuak.sql");
        var gastuKontzeptuakSeedSql = DatuBasea.DatuBaseaSortzeAginduak.IrakurriSortzeAgindua("GastuKontzeptuakSeed.sql");
        var bidaiaSql = DatuBasea.DatuBaseaSortzeAginduak.IrakurriSortzeAgindua("BidaiaTxostenak.sql");
        var gastuLerroSql = DatuBasea.DatuBaseaSortzeAginduak.IrakurriSortzeAgindua("GastuLerroak.sql");
        var auditSql = DatuBasea.DatuBaseaSortzeAginduak.IrakurriSortzeAgindua("AuditoretzaLoga.sql");

        await bezeroa.ExekutatuAsync(gastuKontzeptuakSql, cancellationToken).ConfigureAwait(false);
        await bezeroa.ExekutatuAsync(gastuKontzeptuakSeedSql, cancellationToken).ConfigureAwait(false);
        await bezeroa.ExekutatuAsync(bidaiaSql, cancellationToken).ConfigureAwait(false);
        await bezeroa.ExekutatuAsync(gastuLerroSql, cancellationToken).ConfigureAwait(false);
        await bezeroa.ExekutatuAsync(auditSql, cancellationToken).ConfigureAwait(false);
        await MigraAuditoretzaLogaDiruSarreraIdTxostenIdraTursoAsync(bezeroa, cancellationToken).ConfigureAwait(false);
        await MigraAuditoretzaLogaLangileIdGehituTursoAsync(bezeroa, cancellationToken).ConfigureAwait(false);
        await bezeroa.ExekutatuAsync(
            "CREATE UNIQUE INDEX IF NOT EXISTS idx_erabiltzaileak_dni ON Erabiltzaileak(DNI);",
            cancellationToken).ConfigureAwait(false);
    }

    private async Task MigraAuditoretzaLogaDiruSarreraIdTxostenIdraSqliteAsync()
    {
        var dagoDiruSarrera = await _sqliteKonexioa!.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM pragma_table_info('AuditoretzaLoga') WHERE name = 'DiruSarreraId'").ConfigureAwait(false);
        if (dagoDiruSarrera == 0)
            return;

        await _sqliteKonexioa.ExecuteAsync("PRAGMA foreign_keys = OFF").ConfigureAwait(false);
        await _sqliteKonexioa.ExecuteAsync("""
            CREATE TABLE AuditoretzaLoga_berria (
                LogId INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                TxostenId INTEGER,
                ErabiltzaileId INTEGER NOT NULL DEFAULT 0,
                Ekintza TEXT NOT NULL DEFAULT '',
                DataOrdua TEXT NOT NULL DEFAULT '',
                Deskribapena TEXT NOT NULL DEFAULT '',
                IP_Helbidea TEXT NOT NULL DEFAULT '',
                FOREIGN KEY (ErabiltzaileId) REFERENCES Erabiltzaileak(ErabiltzaileId) ON DELETE RESTRICT,
                FOREIGN KEY (TxostenId) REFERENCES BidaiaTxostenak(TxostenId) ON DELETE SET NULL
            );
            """).ConfigureAwait(false);
        await _sqliteKonexioa.ExecuteAsync("""
            INSERT INTO AuditoretzaLoga_berria (LogId, TxostenId, ErabiltzaileId, Ekintza, DataOrdua, Deskribapena, IP_Helbidea)
            SELECT LogId, NULL, ErabiltzaileId, Ekintza, DataOrdua, Deskribapena, IP_Helbidea
            FROM AuditoretzaLoga;
            """).ConfigureAwait(false);
        await _sqliteKonexioa.ExecuteAsync("DROP TABLE AuditoretzaLoga").ConfigureAwait(false);
        await _sqliteKonexioa.ExecuteAsync("ALTER TABLE AuditoretzaLoga_berria RENAME TO AuditoretzaLoga").ConfigureAwait(false);
        await _sqliteKonexioa.ExecuteAsync("PRAGMA foreign_keys = ON").ConfigureAwait(false);
    }

    private static async Task MigraAuditoretzaLogaDiruSarreraIdTxostenIdraTursoAsync(
        ITursoSqlEgikaritzailea bezeroa,
        CancellationToken cancellationToken)
    {
        const string egiaztapenSql = """
            SELECT COUNT(*) AS Kopurua
            FROM pragma_table_info('AuditoretzaLoga')
            WHERE name = 'DiruSarreraId';
            """;
        var emaitza = await bezeroa.ExekutatuAsync(egiaztapenSql, cancellationToken).ConfigureAwait(false);
        var dagoDiruSarrera = emaitza.LerroTestuBalioak.FirstOrDefault() is { Count: > 0 } lerroa
            && long.TryParse(lerroa[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var kopurua)
            && kopurua > 0;
        if (!dagoDiruSarrera)
            return;

        await bezeroa.ExekutatuAsync("PRAGMA foreign_keys = OFF;", cancellationToken).ConfigureAwait(false);
        await bezeroa.ExekutatuAsync("""
            CREATE TABLE AuditoretzaLoga_berria (
                LogId INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                TxostenId INTEGER,
                ErabiltzaileId INTEGER NOT NULL DEFAULT 0,
                Ekintza TEXT NOT NULL DEFAULT '',
                DataOrdua TEXT NOT NULL DEFAULT '',
                Deskribapena TEXT NOT NULL DEFAULT '',
                IP_Helbidea TEXT NOT NULL DEFAULT '',
                FOREIGN KEY (ErabiltzaileId) REFERENCES Erabiltzaileak(ErabiltzaileId) ON DELETE RESTRICT,
                FOREIGN KEY (TxostenId) REFERENCES BidaiaTxostenak(TxostenId) ON DELETE SET NULL
            );
            """, cancellationToken).ConfigureAwait(false);
        await bezeroa.ExekutatuAsync("""
            INSERT INTO AuditoretzaLoga_berria (LogId, TxostenId, ErabiltzaileId, Ekintza, DataOrdua, Deskribapena, IP_Helbidea)
            SELECT LogId, NULL, ErabiltzaileId, Ekintza, DataOrdua, Deskribapena, IP_Helbidea
            FROM AuditoretzaLoga;
            """, cancellationToken).ConfigureAwait(false);
        await bezeroa.ExekutatuAsync("DROP TABLE AuditoretzaLoga;", cancellationToken).ConfigureAwait(false);
        await bezeroa.ExekutatuAsync("ALTER TABLE AuditoretzaLoga_berria RENAME TO AuditoretzaLoga;", cancellationToken).ConfigureAwait(false);
        await bezeroa.ExekutatuAsync("PRAGMA foreign_keys = ON;", cancellationToken).ConfigureAwait(false);
    }

    private async Task MigraAuditoretzaLogaLangileIdGehituSqliteAsync()
    {
        var dagoLangileId = await _sqliteKonexioa!.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM pragma_table_info('AuditoretzaLoga') WHERE name = 'LangileId'").ConfigureAwait(false);
        if (dagoLangileId > 0)
            return;

        await _sqliteKonexioa.ExecuteAsync("ALTER TABLE AuditoretzaLoga ADD COLUMN LangileId INTEGER").ConfigureAwait(false);
        await _sqliteKonexioa.ExecuteAsync("""
            UPDATE AuditoretzaLoga
            SET LangileId = (
                SELECT ErabiltzaileId FROM BidaiaTxostenak
                WHERE BidaiaTxostenak.TxostenId = AuditoretzaLoga.TxostenId
            )
            WHERE TxostenId IS NOT NULL;
            """).ConfigureAwait(false);
        await _sqliteKonexioa.ExecuteAsync("""
            UPDATE AuditoretzaLoga
            SET LangileId = ErabiltzaileId
            WHERE LangileId IS NULL AND ErabiltzaileId > 0;
            """).ConfigureAwait(false);
    }

    private static async Task MigraAuditoretzaLogaLangileIdGehituTursoAsync(
        ITursoSqlEgikaritzailea bezeroa,
        CancellationToken cancellationToken)
    {
        const string egiaztapenSql = """
            SELECT COUNT(*) AS Kopurua
            FROM pragma_table_info('AuditoretzaLoga')
            WHERE name = 'LangileId';
            """;
        var emaitza = await bezeroa.ExekutatuAsync(egiaztapenSql, cancellationToken).ConfigureAwait(false);
        var dagoLangileId = emaitza.LerroTestuBalioak.FirstOrDefault() is { Count: > 0 } lerroa
            && long.TryParse(lerroa[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var kopurua)
            && kopurua > 0;
        if (dagoLangileId)
            return;

        await bezeroa.ExekutatuAsync("ALTER TABLE AuditoretzaLoga ADD COLUMN LangileId INTEGER;", cancellationToken)
            .ConfigureAwait(false);
        await bezeroa.ExekutatuAsync("""
            UPDATE AuditoretzaLoga
            SET LangileId = (
                SELECT ErabiltzaileId FROM BidaiaTxostenak
                WHERE BidaiaTxostenak.TxostenId = AuditoretzaLoga.TxostenId
            )
            WHERE TxostenId IS NOT NULL;
            """, cancellationToken).ConfigureAwait(false);
        await bezeroa.ExekutatuAsync("""
            UPDATE AuditoretzaLoga
            SET LangileId = ErabiltzaileId
            WHERE LangileId IS NULL AND ErabiltzaileId > 0;
            """, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TxostenOnarpenLaburpena>> ZerrendatuTxostenGuztiekAsync(
        int? sektoreId = null,
        CancellationToken cancellationToken = default)
    {
        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (_urrunTursoModua)
            return await ZerrendatuTxostenGuztiekTursoAsync(sektoreId, cancellationToken).ConfigureAwait(false);

        var (sql, parametroak) = TxostenLaburpenaKontsulta.SortuZerrendaSql(
            adminTestuaSartu: true,
            egoeraIragazkia: null,
            sektoreId: sektoreId,
            erabiltzaileId: null,
            ordenatuSorkuntzaData: true);
        return await _sqliteKonexioa!.QueryAsync<TxostenOnarpenLaburpena>(sql, parametroak).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<TxostenOnarpenLaburpena>> ZerrendatuTxostenGuztiekTursoAsync(
        int? sektoreId,
        CancellationToken cancellationToken)
    {
        return await ExekutatuTursoanAsync(async bezeroa =>
        {
            var (sql, parametroak) = TxostenLaburpenaKontsulta.SortuZerrendaSql(
                adminTestuaSartu: true,
                egoeraIragazkia: null,
                sektoreId: sektoreId,
                erabiltzaileId: null,
                ordenatuSorkuntzaData: true);
            var emaitza = await ExekutatuTursoSqlParametroekinAsync(bezeroa, sql, parametroak, cancellationToken)
                .ConfigureAwait(false);
            return TursoLerroMapatzailea.MapeatuTxostenGuztiekLaburrak(emaitza);
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<BidaiaTxostena?> EskuratuBidaiaTxostenaIdzAsync(int txostenId, CancellationToken cancellationToken = default)
    {
        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (txostenId <= 0)
            return null;

        if (_urrunTursoModua)
        {
            return await ExekutatuTursoanAsync(async bezeroa =>
            {
                const string sql = "SELECT * FROM BidaiaTxostenak WHERE TxostenId = ? LIMIT 1;";
                var emaitza = await bezeroa.ExekutatuAsync(sql, cancellationToken, LibsqlLoturaNormalizatua(txostenId)).ConfigureAwait(false);
                return TursoLerroMapatzailea.MapeatuBidaiaTxostenaLehena(emaitza);
            }, cancellationToken).ConfigureAwait(false);
        }

        var zerrenda = await _sqliteKonexioa!.QueryAsync<BidaiaTxostena>(
            "SELECT * FROM BidaiaTxostenak WHERE TxostenId = ? LIMIT 1",
            txostenId).ConfigureAwait(false);
        return NormalizatuBidaiaTxostenaDataOrduak(zerrenda.FirstOrDefault());
    }

    public async Task<IReadOnlyList<GastuLerroa>> ZerrendatuGastuLerroakTxostenIdzAsync(int txostenId, CancellationToken cancellationToken = default)
    {
        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (txostenId <= 0)
            return Array.Empty<GastuLerroa>();

        if (_urrunTursoModua)
        {
            return await ExekutatuTursoanAsync(async bezeroa =>
            {
                const string sql = "SELECT * FROM GastuLerroak WHERE TxostenId = ? ORDER BY GastuData DESC;";
                var emaitza = await bezeroa.ExekutatuAsync(sql, cancellationToken, LibsqlLoturaNormalizatua(txostenId)).ConfigureAwait(false);
                return TursoLerroMapatzailea.MapeatuGastuLerroZerrenda(emaitza);
            }, cancellationToken).ConfigureAwait(false);
        }

        var lerroak = await _sqliteKonexioa!.QueryAsync<GastuLerroa>(
            "SELECT * FROM GastuLerroak WHERE TxostenId = ? ORDER BY GastuData DESC",
            txostenId).ConfigureAwait(false);
        NormalizatuGastuLerroDataOrduak(lerroak);
        return lerroak;
    }

    public async Task<IReadOnlyList<TxostenOnarpenLaburpena>> ZerrendatuTxostenOnarpenLaburrakAsync(
        string? egoeraIragazkia,
        int? sektoreId = null,
        CancellationToken cancellationToken = default)
    {
        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (_urrunTursoModua)
            return await ZerrendatuTxostenOnarpenLaburrakTursoAsync(egoeraIragazkia, sektoreId, cancellationToken).ConfigureAwait(false);

        var (sql, parametroak) = TxostenLaburpenaKontsulta.SortuZerrendaSql(
            adminTestuaSartu: false,
            egoeraIragazkia: egoeraIragazkia,
            sektoreId: sektoreId,
            erabiltzaileId: null,
            ordenatuSorkuntzaData: true);
        return await _sqliteKonexioa!.QueryAsync<TxostenOnarpenLaburpena>(sql, parametroak).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<TxostenOnarpenLaburpena>> ZerrendatuTxostenOnarpenLaburrakTursoAsync(
        string? egoeraIragazkia,
        int? sektoreId,
        CancellationToken cancellationToken)
    {
        return await ExekutatuTursoanAsync(async bezeroa =>
        {
            var (sql, parametroak) = TxostenLaburpenaKontsulta.SortuZerrendaSql(
                adminTestuaSartu: false,
                egoeraIragazkia: egoeraIragazkia,
                sektoreId: sektoreId,
                erabiltzaileId: null,
                ordenatuSorkuntzaData: true);
            var emaitza = await ExekutatuTursoSqlParametroekinAsync(bezeroa, sql, parametroak, cancellationToken)
                .ConfigureAwait(false);
            return TursoLerroMapatzailea.MapeatuTxostenOnarpenLaburrak(emaitza, egoeraIragazkia);
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<TursoHttpExekuzioarenEmaitza> ExekutatuTursoSqlParametroekinAsync(
        ITursoSqlEgikaritzailea bezeroa,
        string sql,
        object[] parametroak,
        CancellationToken cancellationToken)
    {
        var sqlAmaiera = sql.TrimEnd().EndsWith(';') ? sql : sql + ";";
        if (parametroak.Length == 0)
            return await bezeroa.ExekutatuAsync(sqlAmaiera, cancellationToken).ConfigureAwait(false);

        var tursoParametroak = new object[parametroak.Length];
        for (var i = 0; i < parametroak.Length; i++)
            tursoParametroak[i] = LibsqlLoturaNormalizatua(parametroak[i]);

        return await bezeroa.ExekutatuAsync(sqlAmaiera, cancellationToken, tursoParametroak).ConfigureAwait(false);
    }

    public async Task<BidaiaTxostena?> EskuratuBidaiaTxostenaAdministratzailearentzatAsync(
        int txostenId,
        int? sektoreIragazkia,
        CancellationToken cancellationToken = default)
    {
        var txostena = await EskuratuBidaiaTxostenaIdzAsync(txostenId, cancellationToken).ConfigureAwait(false);
        if (txostena is null)
            return null;

        if (sektoreIragazkia is > 0)
        {
            var batDator = await ErabiltzaileaSektorearekinBatDatorAsync(
                txostena.ErabiltzaileId,
                sektoreIragazkia.Value,
                cancellationToken).ConfigureAwait(false);
            if (!batDator)
                return null;
        }

        return txostena;
    }

    public async Task EguneratuTxostenEgoeraAdministratzaileAsync(
        int txostenId,
        string egoeraBerria,
        string? adminOharra,
        int administratzaileErabiltzaileId,
        int? sektoreIragazkia = null,
        CancellationToken cancellationToken = default)
    {
        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var txostena = await EskuratuBidaiaTxostenaAdministratzailearentzatAsync(
                txostenId,
                sektoreIragazkia,
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Txostena ez da aurkitu edo ez duzu baimenik.");

        var adminErabiltzailea = await BilatuErabiltzaileaIdzAsync(administratzaileErabiltzaileId, cancellationToken).ConfigureAwait(false);
        var adminDNI = adminErabiltzailea?.DNI;

        var orain = DataOrduaBalioak.DataOrduaOrain();
        txostena.Egoera = egoeraBerria;
        txostena.AdminDNI = string.IsNullOrWhiteSpace(adminDNI) ? null : adminDNI.Trim();
        txostena.AdminOharra = string.Equals(egoeraBerria, TxostenEgoera.Ukatua, StringComparison.Ordinal)
            ? (string.IsNullOrWhiteSpace(adminOharra) ? string.Empty : adminOharra.Trim())
            : null;
        txostena.AzkenEguneratzea = orain;
        txostena.DataAprobazioa = string.Equals(egoeraBerria, TxostenEgoera.Onartua, StringComparison.Ordinal)
            ? orain
            : string.Empty;

        if (_urrunTursoModua)
        {
            await ExekutatuTursoanAsync(async bezeroa =>
            {
                const string sql = """
                    UPDATE BidaiaTxostenak
                    SET Egoera = ?, AdminOharra = ?, AdminDNI = ?, AzkenEguneraketa = ?, DataAprobazioa = ?
                    WHERE TxostenId = ?;
                    """;
                await bezeroa.ExekutatuAsync(
                    sql,
                    cancellationToken,
                    LibsqlLoturaNormalizatua(txostena.Egoera),
                    LibsqlLoturaNormalizatua(txostena.AdminOharra ?? string.Empty),
                    LibsqlLoturaNormalizatua(txostena.AdminDNI),
                    LibsqlLoturaNormalizatua(txostena.AzkenEguneratzea),
                    LibsqlLoturaNormalizatua(txostena.DataAprobazioa),
                    LibsqlLoturaNormalizatua(txostenId)).ConfigureAwait(false);
                return 0;
            }, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _sqliteKonexioa!.UpdateAsync(txostena).ConfigureAwait(false);
        }

    }

    public async Task EguneratuTxostenIbilgailuaEtaKilometroakAsync(
        int txostenId,
        int empresaIbilgailua,
        string garraioBidea,
        double kilometroak,
        CancellationToken cancellationToken = default)
    {
        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (txostenId <= 0)
            throw new ArgumentException("Txosten ID baliogabea.", nameof(txostenId));

        var orain = DataOrduaBalioak.DataOrduaOrain();
        var garraioGarbia = garraioBidea.Trim();
        var kilometroGarbiak = kilometroak < 0 ? 0 : Math.Round(kilometroak, 2, MidpointRounding.AwayFromZero);

        if (_urrunTursoModua)
        {
            await ExekutatuTursoanAsync(async bezeroa =>
            {
                const string txostenSql = """
                    UPDATE BidaiaTxostenak
                    SET EmpresaIbilgailua = ?, AzkenEguneraketa = ?
                    WHERE TxostenId = ?;
                    """;
                const string gastuSql = """
                    UPDATE GastuLerroak
                    SET GarraioBidea = ?, Kilometroak = ?
                    WHERE TxostenId = ?
                      AND (IbilgailuaBeharrezkoa = 1
                           OR TRIM(COALESCE(GarraioBidea, '')) IN (?, ?));
                    """;
                await bezeroa.ExekutatuBatchAsync(
                    new (string, object?[])[]
                    {
                        (txostenSql, new object?[]
                        {
                            LibsqlLoturaNormalizatua(empresaIbilgailua),
                            LibsqlLoturaNormalizatua(orain),
                            LibsqlLoturaNormalizatua(txostenId)
                        }),
                        (gastuSql, new object?[]
                        {
                            LibsqlLoturaNormalizatua(garraioGarbia),
                            LibsqlLoturaNormalizatua(kilometroGarbiak),
                            LibsqlLoturaNormalizatua(txostenId),
                            LibsqlLoturaNormalizatua(GarraioBideaBalioak.EnpresakoIbilgailua),
                            LibsqlLoturaNormalizatua(GarraioBideaBalioak.NorberarenIbilgailua)
                        })
                    },
                    cancellationToken).ConfigureAwait(false);
                return 0;
            }, cancellationToken).ConfigureAwait(false);
            return;
        }

        await _sqliteKonexioa!.ExecuteAsync(
            "UPDATE BidaiaTxostenak SET EmpresaIbilgailua = ?, AzkenEguneraketa = ? WHERE TxostenId = ?",
            empresaIbilgailua,
            orain,
            txostenId).ConfigureAwait(false);

        await _sqliteKonexioa.ExecuteAsync(
            """
            UPDATE GastuLerroak
            SET GarraioBidea = ?, Kilometroak = ?
            WHERE TxostenId = ?
              AND (IbilgailuaBeharrezkoa = 1
                   OR TRIM(COALESCE(GarraioBidea, '')) IN (?, ?))
            """,
            garraioGarbia,
            kilometroGarbiak,
            txostenId,
            GarraioBideaBalioak.EnpresakoIbilgailua,
            GarraioBideaBalioak.NorberarenIbilgailua).ConfigureAwait(false);
    }

    public Task IdazkiAuditoretzaLogaZerbitzuraAsync(
        string ekintza,
        string deskribapena,
        int erabiltzaileId,
        int? txostenId = null,
        int? langileId = null,
        CancellationToken cancellationToken = default) =>
        IdatziAuditoretzaLogaAsync(ekintza, deskribapena, erabiltzaileId, txostenId, langileId, cancellationToken);

    private async Task IdatziAuditoretzaLogaAsync(
        string ekintza,
        string deskribapena,
        int erabiltzaileId,
        int? txostenId = null,
        int? langileId = null,
        CancellationToken cancellationToken = default)
    {
        var loga = new AuditoretzaLoga
        {
            TxostenId = txostenId,
            ErabiltzaileId = erabiltzaileId,
            LangileId = langileId,
            Ekintza = ekintza,
            DataOrdua = DataOrduaBalioak.OrduaUtcOrain(),
            Deskribapena = deskribapena,
            IpHelbidea = LortuGailuIpHelbidea()
        };

        if (_urrunTursoModua)
        {
            await ExekutatuTursoanAsync(async bezeroa =>
            {
                const string sql = """
                    INSERT INTO AuditoretzaLoga (TxostenId, ErabiltzaileId, LangileId, Ekintza, DataOrdua, Deskribapena, IP_Helbidea)
                    VALUES (?, ?, ?, ?, ?, ?, ?);
                    """;
                await bezeroa.ExekutatuAsync(
                        sql,
                        cancellationToken,
                        LibsqlLoturaNormalizatua(loga.TxostenId),
                        LibsqlLoturaNormalizatua(loga.ErabiltzaileId),
                        LibsqlLoturaNormalizatua(loga.LangileId),
                        LibsqlLoturaNormalizatua(loga.Ekintza),
                        LibsqlLoturaNormalizatua(DataOrduaBalioak.DataOrduaOsatu(loga.DataOrdua)),
                        LibsqlLoturaNormalizatua(loga.Deskribapena),
                        LibsqlLoturaNormalizatua(loga.IpHelbidea))
                    .ConfigureAwait(false);
                return 0;
            }, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _sqliteKonexioa!.InsertAsync(loga).ConfigureAwait(false);
        }
    }

    private string LortuGailuIpHelbidea()
    {
        try
        {
            foreach (var interfazea in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (interfazea.OperationalStatus != OperationalStatus.Up)
                    continue;

                if (interfazea.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                foreach (var helbidea in interfazea.GetIPProperties().UnicastAddresses)
                {
                    if (helbidea.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;

                    if (IPAddress.IsLoopback(helbidea.Address))
                        continue;

                    return helbidea.Address.ToString();
                }
            }
        }
        catch (NetworkInformationException ex)
        {
            _logger.LogDebug(ex, "AuditoretzaLoga: ezin izan da gailuaren IP helbidea eskuratu.");
        }
        catch (SocketException ex)
        {
            _logger.LogDebug(ex, "AuditoretzaLoga: socket errorea IP helbidea eskuratzean.");
        }

        return string.Empty;
    }

    private static BidaiaTxostena? NormalizatuBidaiaTxostenaDataOrduak(BidaiaTxostena? txostena)
    {
        if (txostena is null)
            return null;

        txostena.HasieraData = DataOrduaBalioak.DataOrduaOsatu(txostena.HasieraData);
        txostena.AmaieraData = DataOrduaBalioak.DataOrduaOsatu(txostena.AmaieraData);
        if (!string.IsNullOrWhiteSpace(txostena.SorkuntzaData))
            txostena.SorkuntzaData = DataOrduaBalioak.DataOrduaOsatu(txostena.SorkuntzaData);
        if (!string.IsNullOrWhiteSpace(txostena.AzkenEguneratzea))
            txostena.AzkenEguneratzea = DataOrduaBalioak.DataOrduaOsatu(txostena.AzkenEguneratzea);
        if (!string.IsNullOrWhiteSpace(txostena.DataAprobazioa))
            txostena.DataAprobazioa = DataOrduaBalioak.DataOrduaOsatu(txostena.DataAprobazioa);

        return txostena;
    }

    private static void NormalizatuGastuLerroDataOrduak(IEnumerable<GastuLerroa> lerroak)
    {
        foreach (var lerroa in lerroak)
            lerroa.GastuData = DataOrduaBalioak.DataOrduaOsatu(lerroa.GastuData);
    }

#if DEBUG
    private async Task AdministratzaileProbakoDatuakSQLiteAsync()
    {
        try
        {
            var txostenKop = await _sqliteKonexioa!.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM BidaiaTxostenak").ConfigureAwait(false);
            if (txostenKop > 0)
                return;

            var langileak = await _sqliteKonexioa.QueryAsync<Erabiltzailea>(
                "SELECT * FROM Erabiltzaileak WHERE Rola = ? LIMIT 1",
                (int)ErabiltzaileRola.Langilea).ConfigureAwait(false);
            var langilea = langileak.FirstOrDefault();
            if (langilea is null)
                return;

            var orain = DataOrduaBalioak.DataOrduaOrain();

            var txostena = new BidaiaTxostena
            {
                ErabiltzaileId = langilea.Id,
                LangileDNI = langilea.DNI,
                Saila = SektoreaKargoarenHiztegia.LortuBaliozkotutakoSailaTestua(langilea.Sektorea) is { Length: > 0 } saila
                    ? saila
                    : "Finantzak",
                Helmuga = "Donostia",
                BidaiaHelburua = "Probako txostena",
                HasieraData = orain,
                AmaieraData = orain,
                PertsonaKopurua = 1,
                JasoAurrekina = 0,
                Egoera = TxostenEgoera.Zain,
                AdminOharra = null,
                MonetaKodea = "EUR",
                SorkuntzaData = orain,
                AzkenEguneratzea = orain,
                DataAprobazioa = string.Empty
            };

            await _sqliteKonexioa.InsertAsync(txostena).ConfigureAwait(false);

            await _sqliteKonexioa.InsertAsync(new GastuLerroa
            {
                TxostenId = txostena.TxostenId,
                KategoriaId = 1,
                GastuData = orain,
                GarraioBidea = "-",
                ZenbatekoaGuztira = 42.5,
                Kilometroak = 0,
                TicketArgazkia = string.Empty,
                Oharrak = "Probako gastua",
                KontzeptuId = 0
            }).ConfigureAwait(false);

            _logger.LogInformation("Garapeneko probako txostena sortu da SQLite-n.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Administratzaile probako datuak (SQLite): errorea.");
        }
    }

    private async Task AdministratzaileProbakoDatuakTursoAsync(ITursoSqlEgikaritzailea bezeroa, CancellationToken cancellationToken)
    {
        try
        {
            var kopEmaitza = await bezeroa.ExekutatuAsync("SELECT COUNT(*) AS c FROM BidaiaTxostenak;", cancellationToken).ConfigureAwait(false);
            var kop = IrakurriKontagailuLehena(kopEmaitza);
            if (kop > 0)
                return;

            var langEmaitza = await bezeroa.ExekutatuAsync(
                "SELECT ErabiltzaileId FROM Erabiltzaileak WHERE Rola = ? LIMIT 1;",
                cancellationToken,
                LibsqlLoturaNormalizatua((int)ErabiltzaileRola.Langilea)).ConfigureAwait(false);

            var langileLerro = langEmaitza.LerroTestuBalioak.FirstOrDefault();
            if (langileLerro is null || langileLerro.Count == 0)
                return;

            var langileIdStr = langileLerro[0];
            if (!int.TryParse(langileIdStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var langileId))
                return;

            var langileEmaitza = await bezeroa.ExekutatuAsync(
                "SELECT DNI, Sektorea FROM Erabiltzaileak WHERE ErabiltzaileId = ? LIMIT 1;",
                cancellationToken,
                LibsqlLoturaNormalizatua(langileId)).ConfigureAwait(false);
            var langileLerroMapa = langileEmaitza.LerroTestuBalioak.FirstOrDefault();
            Dictionary<string, string>? langileMapa = null;
            if (langileLerroMapa is not null && langileLerroMapa.Count > 0)
                langileMapa = TursoLerroMapatzailea.SortuTursoLerroMapa(langileEmaitza.ZutabeIzenak, langileLerroMapa);
            var langileDni = langileMapa is not null
                ? IrakurriMapaTestuaLehenetsia(langileMapa, "DNI")
                : string.Empty;
            var sailaProbako = langileMapa is not null
                ? SektoreaKargoarenHiztegia.LortuBaliozkotutakoSailaTestua(IrakurriMapaTestuaLehenetsia(langileMapa, "Sektorea"))
                : string.Empty;
            if (string.IsNullOrEmpty(sailaProbako))
                sailaProbako = "Finantzak";

            var orain = DataOrduaBalioak.DataOrduaOrain();

            var txostenEmaitza = await bezeroa.ExekutatuAsync(
                """
                INSERT INTO BidaiaTxostenak (
                  ErabiltzaileId, LangileDNI, Saila, Helmuga, BidaiaHelburua,
                  HasieraData, AmaieraData, PertsonaKopurua, JasoAurrerakina, Egoera, AdminOharra,
                  MonetaKodea, SorkuntzaData, AzkenEguneraketa, DataAprobazioa
                ) VALUES (?, ?, ?, 'Donostia', 'Probako txostena', ?, ?, 1, 0, ?, NULL, 'EUR', ?, ?, '');
                """,
                cancellationToken,
                LibsqlLoturaNormalizatua(langileId),
                LibsqlLoturaNormalizatua(langileDni),
                LibsqlLoturaNormalizatua(sailaProbako),
                LibsqlLoturaNormalizatua(orain),
                LibsqlLoturaNormalizatua(orain),
                LibsqlLoturaNormalizatua(TxostenEgoera.Zain),
                LibsqlLoturaNormalizatua(orain),
                LibsqlLoturaNormalizatua(orain)).ConfigureAwait(false);

            await bezeroa.ExekutatuAsync(
                """
                INSERT INTO GastuLerroak (
                  TxostenId, KategoriaId, GastuData, GarraioBidea, Zenbatekoa_Guztira,
                  Kilometroak, TicketArgazkiBidea, Oharrak, KontzeptuId
                ) VALUES (?, 1, ?, '-', 42.5, 0, '', 'Probako gastua', 0);
                """,
                cancellationToken,
                LibsqlLoturaNormalizatua(txostenEmaitza.AzkenTxertatutakoErrenkadaId),
                LibsqlLoturaNormalizatua(orain)).ConfigureAwait(false);

            _logger.LogInformation("Garapeneko probako txostena sortu da Turson.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Administratzaile probako datuak (Turso): errorea.");
        }
    }
#endif
}
