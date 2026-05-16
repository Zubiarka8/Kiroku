using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.DatuEreduak;
using Kirokuu.Zerbitzuak;
using Microsoft.Extensions.Logging;
using SQLite;

namespace Kirokuu.ZerbitzuakSaioa;

public sealed partial class DatuBaseaZerbitzua
{
    private const string AuditoretzaEkintzaTxostenaOnartu = "TxostenaOnartua";

    private const string AuditoretzaEkintzaTxostenaUkatu = "TxostenaEzeztatu";

    private const string AuditoretzaEkintzaTxostenaEskatuDu = "TxostenaEskatuDu";

    private static string SektoreaTestuaIdentifikatzailetik(int identifikatzailea) => identifikatzailea switch
    {
        (int)EnpresakoSektorea.Finantzak => "Finantzak",
        (int)EnpresakoSektorea.Marketina => "Marketina",
        (int)EnpresakoSektorea.Salmentak => "Salmentak",
        _ => string.Empty
    };

    private async Task BermatuTursoGainerakoTaulakEtaZutabeakAsync(ITursoSqlEgikaritzailea bezeroa, CancellationToken cancellationToken)
    {
        await bezeroa.ExekutatuAsync("PRAGMA foreign_keys = ON;", cancellationToken).ConfigureAwait(false);

        const string gastuKontzeptuakSql = """
            CREATE TABLE IF NOT EXISTS GastuKontzeptuak (
                KategoriaId INTEGER PRIMARY KEY NOT NULL,
                Izena TEXT NOT NULL DEFAULT '',
                Deskribapena TEXT NOT NULL DEFAULT '',
                IbilgailuaBeharrezkoa INTEGER NOT NULL DEFAULT 0,
                Estatusa TEXT NOT NULL DEFAULT '',
                GastuKontzeptuId INTEGER NOT NULL DEFAULT 0
            );
            """;

        const string bidaiaSql = """
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
            """;

        const string gastuLerroSql = """
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
            """;

        const string auditSql = """
            CREATE TABLE IF NOT EXISTS AuditoretzaLoga (
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
            """;

        const string gastuKontzeptuakSeedSql = """
            INSERT OR IGNORE INTO GastuKontzeptuak
              (KategoriaId, Izena, Deskribapena, IbilgailuaBeharrezkoa, Estatusa, GastuKontzeptuId)
            VALUES
              (1, 'Bazkaria',         'Jangela eta bazkari gastuak',   0, 'Aktibo', 1),
              (2, 'Gasolina',         'Erregai gastuak',               1, 'Aktibo', 2),
              (3, 'Garraio publikoa', 'Autobus, metro eta trena',      0, 'Aktibo', 3),
              (4, 'Hotela',           'Ostatua eta gau-pasak',         0, 'Aktibo', 4),
              (5, 'Peajea',           'Autobide eta tunelak',          1, 'Aktibo', 5),
              (6, 'Aparkalekua',      'Aparkagune gastuak',            1, 'Aktibo', 6),
              (7, 'Bidaia',           'Hegazkin eta garraio nagusiak', 0, 'Aktibo', 7),
              (8, 'Materialak',       'Bulego eta lan materialak',     0, 'Aktibo', 8),
              (9, 'Bestelakoa',       'Sailkatu gabeko gastuak',       0, 'Aktibo', 9);
            """;

        await bezeroa.ExekutatuAsync(gastuKontzeptuakSql, cancellationToken).ConfigureAwait(false);
        await bezeroa.ExekutatuAsync(gastuKontzeptuakSeedSql, cancellationToken).ConfigureAwait(false);
        await bezeroa.ExekutatuAsync(bidaiaSql, cancellationToken).ConfigureAwait(false);
        await bezeroa.ExekutatuAsync(gastuLerroSql, cancellationToken).ConfigureAwait(false);
        await bezeroa.ExekutatuAsync(auditSql, cancellationToken).ConfigureAwait(false);
        await bezeroa.ExekutatuAsync(
            "CREATE UNIQUE INDEX IF NOT EXISTS idx_erabiltzaileak_dni ON Erabiltzaileak(DNI);",
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TxostenOnarpenLaburpena>> ZerrendatuTxostenGuztiekAsync(
        int? sektoreId = null,
        CancellationToken cancellationToken = default)
    {
        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (_urrunTursoModua)
            return await ZerrendatuTxostenGuztiekTursoAsync(sektoreId, cancellationToken).ConfigureAwait(false);

        const string sqlOinarria = """
            SELECT
              b.TxostenId AS TxostenId,
              b.ErabiltzaileId AS ErabiltzaileId,
              TRIM(COALESCE(e.Izena,'') || ' ' || COALESCE(e.Abizena,'')) AS LangileTestua,
              b.Helmuga AS Helmuga,
              b.Egoera AS Egoera,
              COALESCE((SELECT SUM(gl.Zenbatekoa_Guztira) FROM GastuLerroak gl WHERE gl.TxostenId = b.TxostenId), 0) AS GastuenBatuketakoZenbatekoa,
              b.HasieraData AS HasieraData,
              COALESCE(TRIM(COALESCE(a.Izena,'') || ' ' || COALESCE(a.Abizena,'')), '') AS AdminTestua
            FROM BidaiaTxostenak b
            INNER JOIN Erabiltzaileak e ON e.ErabiltzaileId = b.ErabiltzaileId
            LEFT JOIN Erabiltzaileak a ON a.DNI = b.AdminDNI
            """;

        if (sektoreId is > 0)
        {
            var sql = sqlOinarria + " WHERE e.Sektorea = ? ORDER BY b.SorkuntzaData DESC";
            return await _sqliteKonexioa!.QueryAsync<TxostenOnarpenLaburpena>(sql, sektoreId.Value).ConfigureAwait(false);
        }

        return await _sqliteKonexioa!.QueryAsync<TxostenOnarpenLaburpena>(
            sqlOinarria + " ORDER BY b.SorkuntzaData DESC").ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<TxostenOnarpenLaburpena>> ZerrendatuTxostenGuztiekTursoAsync(
        int? sektoreId,
        CancellationToken cancellationToken)
    {
        return await ExekutatuTursoanAsync(async bezeroa =>
        {
            const string sqlOinarria = """
                SELECT
                  b.TxostenId AS TxostenId,
                  b.ErabiltzaileId AS ErabiltzaileId,
                  TRIM(COALESCE(e.Izena,'') || ' ' || COALESCE(e.Abizena,'')) AS LangileTestua,
                  b.Helmuga AS Helmuga,
                  b.Egoera AS Egoera,
                  COALESCE((SELECT SUM(gl.Zenbatekoa_Guztira) FROM GastuLerroak gl WHERE gl.TxostenId = b.TxostenId), 0) AS GastuenBatuketakoZenbatekoa,
                  b.HasieraData AS HasieraData,
                  COALESCE(TRIM(COALESCE(a.Izena,'') || ' ' || COALESCE(a.Abizena,'')), '') AS AdminTestua
                FROM BidaiaTxostenak b
                INNER JOIN Erabiltzaileak e ON e.ErabiltzaileId = b.ErabiltzaileId
                LEFT JOIN Erabiltzaileak a ON a.DNI = b.AdminDNI
                """;

            TursoHttpExekuzioarenEmaitza emaitza;
            if (sektoreId is > 0)
            {
                emaitza = await bezeroa.ExekutatuAsync(
                    sqlOinarria + " WHERE e.Sektorea = ? ORDER BY b.SorkuntzaData DESC;",
                    cancellationToken,
                    LibsqlLoturaNormalizatua(sektoreId.Value)).ConfigureAwait(false);
            }
            else
            {
                emaitza = await bezeroa.ExekutatuAsync(
                    sqlOinarria + " ORDER BY b.SorkuntzaData DESC;",
                    cancellationToken).ConfigureAwait(false);
            }

            return MapeatuTxostenGuztiekLaburrak(emaitza);
        }, cancellationToken).ConfigureAwait(false);
    }

    private static IReadOnlyList<TxostenOnarpenLaburpena> MapeatuTxostenGuztiekLaburrak(
        TursoHttpExekuzioarenEmaitza emaitza)
    {
        var zutabeak = emaitza.ZutabeIzenak;
        var zerrenda = new List<TxostenOnarpenLaburpena>();
        foreach (var lerroa in emaitza.LerroTestuBalioak)
        {
            var mapa = SortuTursoLerroMapa(zutabeak, lerroa);
            zerrenda.Add(new TxostenOnarpenLaburpena
            {
                TxostenId = IrakurriMapaOsoaLehenetsia(mapa, "TxostenId", 0),
                ErabiltzaileId = IrakurriMapaOsoa(mapa, "ErabiltzaileId"),
                LangileTestua = IrakurriMapaTestuaLehenetsia(mapa, "LangileTestua"),
                Helmuga = IrakurriMapaTestuaLehenetsia(mapa, "Helmuga"),
                Egoera = IrakurriMapaTestuaLehenetsia(mapa, "Egoera"),
                GastuenBatuketakoZenbatekoa = IrakurriMapaKomaHamarkatuaLehenetsia(mapa, "GastuenBatuketakoZenbatekoa", 0),
                HasieraData = DataOrduaBalioak.DataOrduaOsatu(IrakurriMapaTestuaLehenetsia(mapa, "HasieraData")),
                AdminTestua = IrakurriMapaTestuaLehenetsia(mapa, "AdminTestua")
            });
        }

        return zerrenda;
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
                return MapeatuBidaiaTxostenaLehena(emaitza);
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
                return MapeatuGastuLerroZerrenda(emaitza);
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

        var iragazkiEgoera = !string.IsNullOrWhiteSpace(egoeraIragazkia);
        var iragazkiSektorea = sektoreId is > 0;

        string sql;
        if (!iragazkiEgoera && !iragazkiSektorea)
        {
            sql = """
                  SELECT
                    b.TxostenId AS TxostenId,
                    b.ErabiltzaileId AS ErabiltzaileId,
                    TRIM(COALESCE(e.Izena,'') || ' ' || COALESCE(e.Abizena,'')) AS LangileTestua,
                    b.Helmuga AS Helmuga,
                    b.Egoera AS Egoera,
                    COALESCE((SELECT SUM(gl.Zenbatekoa_Guztira) FROM GastuLerroak gl WHERE gl.TxostenId = b.TxostenId), 0) AS GastuenBatuketakoZenbatekoa,
                    b.HasieraData AS HasieraData
                  FROM BidaiaTxostenak b
                  INNER JOIN Erabiltzaileak e ON e.ErabiltzaileId = b.ErabiltzaileId
                  ORDER BY b.SorkuntzaData DESC
                  """;
            return await _sqliteKonexioa!.QueryAsync<TxostenOnarpenLaburpena>(sql).ConfigureAwait(false);
        }

        if (iragazkiEgoera && iragazkiSektorea)
        {
            sql = """
                  SELECT
                    b.TxostenId AS TxostenId,
                    b.ErabiltzaileId AS ErabiltzaileId,
                    TRIM(COALESCE(e.Izena,'') || ' ' || COALESCE(e.Abizena,'')) AS LangileTestua,
                    b.Helmuga AS Helmuga,
                    b.Egoera AS Egoera,
                    COALESCE((SELECT SUM(gl.Zenbatekoa_Guztira) FROM GastuLerroak gl WHERE gl.TxostenId = b.TxostenId), 0) AS GastuenBatuketakoZenbatekoa,
                    b.HasieraData AS HasieraData
                  FROM BidaiaTxostenak b
                  INNER JOIN Erabiltzaileak e ON e.ErabiltzaileId = b.ErabiltzaileId
                  WHERE b.Egoera = ? AND e.Sektorea = ?
                  ORDER BY b.SorkuntzaData DESC
                  """;
            return await _sqliteKonexioa!.QueryAsync<TxostenOnarpenLaburpena>(sql, egoeraIragazkia!.Trim(), SektoreaTestuaIdentifikatzailetik(sektoreId!.Value)).ConfigureAwait(false);
        }

        if (iragazkiSektorea)
        {
            sql = """
                  SELECT
                    b.TxostenId AS TxostenId,
                    b.ErabiltzaileId AS ErabiltzaileId,
                    TRIM(COALESCE(e.Izena,'') || ' ' || COALESCE(e.Abizena,'')) AS LangileTestua,
                    b.Helmuga AS Helmuga,
                    b.Egoera AS Egoera,
                    COALESCE((SELECT SUM(gl.Zenbatekoa_Guztira) FROM GastuLerroak gl WHERE gl.TxostenId = b.TxostenId), 0) AS GastuenBatuketakoZenbatekoa,
                    b.HasieraData AS HasieraData
                  FROM BidaiaTxostenak b
                  INNER JOIN Erabiltzaileak e ON e.ErabiltzaileId = b.ErabiltzaileId
                  WHERE e.Sektorea = ?
                  ORDER BY b.SorkuntzaData DESC
                  """;
            return await _sqliteKonexioa!.QueryAsync<TxostenOnarpenLaburpena>(sql, SektoreaTestuaIdentifikatzailetik(sektoreId!.Value)).ConfigureAwait(false);
        }

        sql = """
              SELECT
                b.TxostenId AS TxostenId,
                b.ErabiltzaileId AS ErabiltzaileId,
                TRIM(COALESCE(e.Izena,'') || ' ' || COALESCE(e.Abizena,'')) AS LangileTestua,
                b.Helmuga AS Helmuga,
                b.Egoera AS Egoera,
                COALESCE((SELECT SUM(gl.Zenbatekoa_Guztira) FROM GastuLerroak gl WHERE gl.TxostenId = b.TxostenId), 0) AS GastuenBatuketakoZenbatekoa,
                b.HasieraData AS HasieraData
              FROM BidaiaTxostenak b
              INNER JOIN Erabiltzaileak e ON e.ErabiltzaileId = b.ErabiltzaileId
              WHERE b.Egoera = ?
              ORDER BY b.SorkuntzaData DESC
              """;
        return await _sqliteKonexioa!.QueryAsync<TxostenOnarpenLaburpena>(sql, egoeraIragazkia!.Trim()).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<TxostenOnarpenLaburpena>> ZerrendatuTxostenOnarpenLaburrakTursoAsync(
        string? egoeraIragazkia,
        int? sektoreId,
        CancellationToken cancellationToken)
    {
        return await ExekutatuTursoanAsync(async bezeroa =>
        {
            var iragazkiEgoera = !string.IsNullOrWhiteSpace(egoeraIragazkia);
            var iragazkiSektorea = sektoreId is > 0;

            string sql;
            TursoHttpExekuzioarenEmaitza emaitza;

            if (!iragazkiEgoera && !iragazkiSektorea)
            {
                sql = """
                      SELECT
                        b.TxostenId AS TxostenId,
                        b.ErabiltzaileId AS ErabiltzaileId,
                        TRIM(COALESCE(e.Izena,'') || ' ' || COALESCE(e.Abizena,'')) AS LangileTestua,
                        b.Helmuga AS Helmuga,
                        b.Egoera AS Egoera,
                        COALESCE((SELECT SUM(gl.Zenbatekoa_Guztira) FROM GastuLerroak gl WHERE gl.TxostenId = b.TxostenId), 0) AS GastuenBatuketakoZenbatekoa,
                        b.HasieraData AS HasieraData
                      FROM BidaiaTxostenak b
                      INNER JOIN Erabiltzaileak e ON e.ErabiltzaileId = b.ErabiltzaileId
                      ORDER BY b.SorkuntzaData DESC;
                      """;
                emaitza = await bezeroa.ExekutatuAsync(sql, cancellationToken).ConfigureAwait(false);
            }
            else if (iragazkiEgoera && iragazkiSektorea)
            {
                sql = """
                      SELECT
                        b.TxostenId AS TxostenId,
                        b.ErabiltzaileId AS ErabiltzaileId,
                        TRIM(COALESCE(e.Izena,'') || ' ' || COALESCE(e.Abizena,'')) AS LangileTestua,
                        b.Helmuga AS Helmuga,
                        b.Egoera AS Egoera,
                        COALESCE((SELECT SUM(gl.Zenbatekoa_Guztira) FROM GastuLerroak gl WHERE gl.TxostenId = b.TxostenId), 0) AS GastuenBatuketakoZenbatekoa,
                        b.HasieraData AS HasieraData
                      FROM BidaiaTxostenak b
                      INNER JOIN Erabiltzaileak e ON e.ErabiltzaileId = b.ErabiltzaileId
                      WHERE b.Egoera = ? AND e.Sektorea = ?
                      ORDER BY b.SorkuntzaData DESC;
                      """;
                emaitza = await bezeroa.ExekutatuAsync(sql, cancellationToken,
                    LibsqlLoturaNormalizatua(egoeraIragazkia!.Trim()),
                    LibsqlLoturaNormalizatua(SektoreaTestuaIdentifikatzailetik(sektoreId!.Value))).ConfigureAwait(false);
            }
            else if (iragazkiSektorea)
            {
                sql = """
                      SELECT
                        b.TxostenId AS TxostenId,
                        b.ErabiltzaileId AS ErabiltzaileId,
                        TRIM(COALESCE(e.Izena,'') || ' ' || COALESCE(e.Abizena,'')) AS LangileTestua,
                        b.Helmuga AS Helmuga,
                        b.Egoera AS Egoera,
                        COALESCE((SELECT SUM(gl.Zenbatekoa_Guztira) FROM GastuLerroak gl WHERE gl.TxostenId = b.TxostenId), 0) AS GastuenBatuketakoZenbatekoa,
                        b.HasieraData AS HasieraData
                      FROM BidaiaTxostenak b
                      INNER JOIN Erabiltzaileak e ON e.ErabiltzaileId = b.ErabiltzaileId
                      WHERE e.Sektorea = ?
                      ORDER BY b.SorkuntzaData DESC;
                      """;
                emaitza = await bezeroa.ExekutatuAsync(sql, cancellationToken,
                    LibsqlLoturaNormalizatua(SektoreaTestuaIdentifikatzailetik(sektoreId!.Value))).ConfigureAwait(false);
            }
            else
            {
                sql = """
                      SELECT
                        b.TxostenId AS TxostenId,
                        b.ErabiltzaileId AS ErabiltzaileId,
                        TRIM(COALESCE(e.Izena,'') || ' ' || COALESCE(e.Abizena,'')) AS LangileTestua,
                        b.Helmuga AS Helmuga,
                        b.Egoera AS Egoera,
                        COALESCE((SELECT SUM(gl.Zenbatekoa_Guztira) FROM GastuLerroak gl WHERE gl.TxostenId = b.TxostenId), 0) AS GastuenBatuketakoZenbatekoa,
                        b.HasieraData AS HasieraData
                      FROM BidaiaTxostenak b
                      INNER JOIN Erabiltzaileak e ON e.ErabiltzaileId = b.ErabiltzaileId
                      WHERE b.Egoera = ?
                      ORDER BY b.SorkuntzaData DESC;
                      """;
                emaitza = await bezeroa.ExekutatuAsync(sql, cancellationToken,
                    LibsqlLoturaNormalizatua(egoeraIragazkia!.Trim())).ConfigureAwait(false);
            }

            return MapeatuTxostenOnarpenLaburrak(emaitza, egoeraIragazkia);
        }, cancellationToken).ConfigureAwait(false);
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

        var orain = DataOrduaBalioak.DataOrduaOsatu(DateTime.UtcNow);
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

        var ekintza = string.Equals(egoeraBerria, TxostenEgoera.Onartua, StringComparison.Ordinal)
            ? AuditoretzaEkintzaTxostenaOnartu
            : AuditoretzaEkintzaTxostenaUkatu;
        var deskribapena = $"{txostenId} · {egoeraBerria}";
        await IdazkiAuditoretzaLogaAsync(ekintza, deskribapena, administratzaileErabiltzaileId, cancellationToken).ConfigureAwait(false);
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

        var orain = DataOrduaBalioak.DataOrduaOsatu(DateTime.UtcNow);
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

    private async Task IdazkiAuditoretzaLogaAsync(
        string ekintza,
        string deskribapena,
        int erabiltzaileId,
        CancellationToken cancellationToken)
    {
        var loga = new AuditoretzaLoga
        {
            DiruSarreraId = null,
            ErabiltzaileId = erabiltzaileId,
            Ekintza = ekintza,
            DataOrdua = DateTime.UtcNow,
            Deskribapena = deskribapena,
            IpHelbidea = LortuGailuIpHelbidea()
        };

        if (_urrunTursoModua)
        {
            await ExekutatuTursoanAsync(async bezeroa =>
            {
                const string sql = """
                    INSERT INTO AuditoretzaLoga (DiruSarreraId, ErabiltzaileId, Ekintza, DataOrdua, Deskribapena, IP_Helbidea)
                    VALUES (?, ?, ?, ?, ?, ?);
                    """;
                await bezeroa.ExekutatuAsync(
                        sql,
                        cancellationToken,
                        LibsqlLoturaNormalizatua(loga.DiruSarreraId),
                        LibsqlLoturaNormalizatua(loga.ErabiltzaileId),
                        LibsqlLoturaNormalizatua(loga.Ekintza),
                        LibsqlLoturaNormalizatua(loga.DataOrdua.ToString("o", CultureInfo.InvariantCulture)),
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

    private static BidaiaTxostena? MapeatuBidaiaTxostenaLehena(TursoHttpExekuzioarenEmaitza emaitza)
    {
        var lerroa = emaitza.LerroTestuBalioak.FirstOrDefault();
        if (lerroa is null)
            return null;

        var zutabeak = emaitza.ZutabeIzenak;
        var mapa = SortuTursoLerroMapa(zutabeak, lerroa);
        return MapeatuBidaiaTxostenaMapatik(mapa);
    }

    private static Dictionary<string, string> SortuTursoLerroMapa(IReadOnlyList<string> zutabeak, IReadOnlyList<string> lerroa)
    {
        var mapa = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < zutabeak.Count && i < lerroa.Count; i++)
            mapa[zutabeak[i]] = TursoTestuaIrakurri(lerroa[i]);

        return mapa;
    }

    private static BidaiaTxostena MapeatuBidaiaTxostenaMapatik(Dictionary<string, string> mapa)
    {
        var adminOharraBalioa = IrakurriMapaTestuaLehenetsia(mapa, "AdminOharra", string.Empty);
        var adminDniBalioa = IrakurriMapaTestuaLehenetsia(mapa, "AdminDNI", string.Empty);
        return new BidaiaTxostena
        {
            TxostenId = IrakurriMapaOsoaLehenetsia(mapa, "TxostenId", 0),
            ErabiltzaileId = IrakurriMapaOsoaLehenetsia(mapa, "ErabiltzaileId", 0),
            LangileDNI = IrakurriMapaTestuaLehenetsia(mapa, "LangileDNI"),
            Saila = IrakurriMapaTestuaLehenetsia(mapa, "Saila"),
            Helmuga = IrakurriMapaTestuaLehenetsia(mapa, "Helmuga"),
            BidaiaHelburua = IrakurriMapaTestuaLehenetsia(mapa, "BidaiaHelburua"),
            HasieraData = DataOrduaBalioak.DataOrduaOsatu(IrakurriMapaTestuaLehenetsia(mapa, "HasieraData")),
            AmaieraData = DataOrduaBalioak.DataOrduaOsatu(IrakurriMapaTestuaLehenetsia(mapa, "AmaieraData")),
            PertsonaKopurua = IrakurriMapaOsoaLehenetsia(mapa, "PertsonaKopurua", 0),
            JasoAurrekina = IrakurriMapaOsoaLehenetsia(mapa, "JasoAurrerakina", 0),
            Egoera = IrakurriMapaTestuaLehenetsia(mapa, "Egoera"),
            AdminOharra = string.IsNullOrEmpty(adminOharraBalioa) ? null : adminOharraBalioa,
            AdminDNI = string.IsNullOrEmpty(adminDniBalioa) ? null : adminDniBalioa,
            EmpresaIbilgailua = IrakurriMapaOsoaLehenetsia(mapa, "EmpresaIbilgailua", 0),
            MonetaKodea = IrakurriMapaTestuaLehenetsia(mapa, "MonetaKodea"),
            SorkuntzaData = IrakurriMapaTestuaLehenetsia(mapa, "SorkuntzaData"),
            AzkenEguneratzea = IrakurriMapaTestuaLehenetsia(mapa, "AzkenEguneraketa"),
            DataAprobazioa = IrakurriMapaTestuaLehenetsia(mapa, "DataAprobazioa")
        };
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

    private static double IrakurriMapaKomaHamarkatuaLehenetsia(Dictionary<string, string> mapa, string gakoa, double lehenetsia)
    {
        if (!mapa.TryGetValue(gakoa, out var testua) || string.IsNullOrWhiteSpace(testua))
            return lehenetsia;

        return double.TryParse(testua, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : lehenetsia;
    }

    private static IReadOnlyList<GastuLerroa> MapeatuGastuLerroZerrenda(TursoHttpExekuzioarenEmaitza emaitza)
    {
        var zutabeak = emaitza.ZutabeIzenak;
        var zerrenda = new List<GastuLerroa>();
        foreach (var lerroa in emaitza.LerroTestuBalioak)
        {
            var mapa = SortuTursoLerroMapa(zutabeak, lerroa);
            zerrenda.Add(new GastuLerroa
            {
                GastuId = IrakurriMapaOsoaLehenetsia(mapa, "GastuId", 0),
                TxostenId = IrakurriMapaOsoaLehenetsia(mapa, "TxostenId", 0),
                KategoriaId = IrakurriMapaOsoaLehenetsia(mapa, "KategoriaId", 0),
                GastuData = DataOrduaBalioak.DataOrduaOsatu(IrakurriMapaTestuaLehenetsia(mapa, "GastuData")),
                GarraioBidea = IrakurriMapaTestuaLehenetsia(mapa, "GarraioBidea"),
                ZenbatekoaGuztira = IrakurriMapaKomaHamarkatuaLehenetsia(mapa, "Zenbatekoa_Guztira", 0),
                Kilometroak = IrakurriMapaKomaHamarkatuaLehenetsia(mapa, "Kilometroak", 0),
                TicketArgazkia = IrakurriMapaTestuaLehenetsia(mapa, "TicketArgazkiBidea"),
                Oharrak = IrakurriMapaTestuaLehenetsia(mapa, "Oharrak"),
                KontzeptuId = IrakurriMapaOsoaLehenetsia(mapa, "KontzeptuId", 0),
                IbilgailuaBeharrezkoa = IrakurriMapaOsoaLehenetsia(mapa, "IbilgailuaBeharrezkoa", 0)
            });
        }

        return zerrenda;
    }

    private static IReadOnlyList<GastuKontzeptua> MapeatuGastuKontzeptuZerrenda(TursoHttpExekuzioarenEmaitza emaitza)
    {
        var zutabeak = emaitza.ZutabeIzenak;
        var zerrenda = new List<GastuKontzeptua>();
        foreach (var lerroa in emaitza.LerroTestuBalioak)
        {
            var mapa = SortuTursoLerroMapa(zutabeak, lerroa);
            zerrenda.Add(new GastuKontzeptua
            {
                KategoriaId = IrakurriMapaOsoaLehenetsia(mapa, "KategoriaId", 0),
                Izena = IrakurriMapaTestuaLehenetsia(mapa, "Izena"),
                Deskribapena = IrakurriMapaTestuaLehenetsia(mapa, "Deskribapena"),
                IbilgailuaBeharDu = IrakurriMapaOsoaLehenetsia(mapa, "IbilgailuaBeharrezkoa", 0),
                Estatusa = IrakurriMapaTestuaLehenetsia(mapa, "Estatusa"),
                GastuKontzeptuId = IrakurriMapaOsoaLehenetsia(mapa, "GastuKontzeptuId", 0)
            });
        }

        return zerrenda;
    }

    private static IReadOnlyList<TxostenOnarpenLaburpena> MapeatuTxostenOnarpenLaburrak(
        TursoHttpExekuzioarenEmaitza emaitza,
        string? egoeraIragazkia)
    {
        var zutabeak = emaitza.ZutabeIzenak;
        var zerrenda = new List<TxostenOnarpenLaburpena>();
        var egoeraBerretsia = string.IsNullOrWhiteSpace(egoeraIragazkia) ? null : egoeraIragazkia.Trim();
        foreach (var lerroa in emaitza.LerroTestuBalioak)
        {
            var mapa = SortuTursoLerroMapa(zutabeak, lerroa);
            var laburpena = new TxostenOnarpenLaburpena
            {
                TxostenId = IrakurriMapaOsoaLehenetsia(mapa, "TxostenId", 0),
                ErabiltzaileId = IrakurriMapaOsoa(mapa, "ErabiltzaileId"),
                LangileTestua = IrakurriMapaTestuaLehenetsia(mapa, "LangileTestua"),
                Helmuga = IrakurriMapaTestuaLehenetsia(mapa, "Helmuga"),
                Egoera = IrakurriMapaTestuaLehenetsia(mapa, "Egoera"),
                GastuenBatuketakoZenbatekoa = IrakurriMapaKomaHamarkatuaLehenetsia(mapa, "GastuenBatuketakoZenbatekoa", 0),
                HasieraData = DataOrduaBalioak.DataOrduaOsatu(IrakurriMapaTestuaLehenetsia(mapa, "HasieraData"))
            };
            if (egoeraBerretsia is not null)
                laburpena.Egoera = egoeraBerretsia;
            zerrenda.Add(laburpena);
        }

        return zerrenda;
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

            var orain = DataOrduaBalioak.DataOrduaOsatu(DateTime.UtcNow);

            var txostena = new BidaiaTxostena
            {
                ErabiltzaileId = langilea.Id,
                LangileDNI = langilea.DNI,
                Saila = "Garapena",
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

            var orain = DataOrduaBalioak.DataOrduaOsatu(DateTime.UtcNow);

            var txostenEmaitza = await bezeroa.ExekutatuAsync(
                """
                INSERT INTO BidaiaTxostenak (
                  ErabiltzaileId, LangileDNI, Saila, Helmuga, BidaiaHelburua,
                  HasieraData, AmaieraData, PertsonaKopurua, JasoAurrerakina, Egoera, AdminOharra,
                  MonetaKodea, SorkuntzaData, AzkenEguneraketa, DataAprobazioa
                ) VALUES (?, '', 'Garapena', 'Donostia', 'Probako txostena', ?, ?, 1, 0, ?, NULL, 'EUR', ?, ?, '');
                """,
                cancellationToken,
                LibsqlLoturaNormalizatua(langileId),
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
