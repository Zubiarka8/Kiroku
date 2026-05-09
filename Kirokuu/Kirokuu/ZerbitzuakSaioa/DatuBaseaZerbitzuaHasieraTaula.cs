using System.Globalization;
using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.DatuEreduak;
using Kirokuu.Zerbitzuak;
using SQLite;

namespace Kirokuu.ZerbitzuakSaioa;

public sealed partial class DatuBaseaZerbitzua
{
    private sealed class GuztiraBatuka
    {
        public double Guztira { get; set; }
    }

    public Task<double> EskuratuOnartutakoGastuenGuztiraLangileAsync(
        int erabiltzaileId,
        CancellationToken cancellationToken = default) =>
        EskuratuOnartutakoGastuenGuztiraBarneanAsync(erabiltzaileId, cancellationToken);

    public Task<double> EskuratuOnartutakoGastuenGuztiraOrguOrokorraAsync(
        CancellationToken cancellationToken = default) =>
        EskuratuOnartutakoGastuenGuztiraBarneanAsync(null, cancellationToken);

    public Task<int> EskuratuZainTxartenKopuruaLangileAsync(
        int erabiltzaileId,
        CancellationToken cancellationToken = default) =>
        EskuratuZainTxartenKopuruaBarneanAsync(erabiltzaileId, cancellationToken);

    public Task<int> EskuratuZainTxartenKopuruaOrguOrokorraAsync(CancellationToken cancellationToken = default) =>
        EskuratuZainTxartenKopuruaBarneanAsync(null, cancellationToken);

    public async Task<IReadOnlyList<HilabetekoGastuAgregatua>> EskuratuAzkenHilabeteetakoOnartutakoGastuakLangileAsync(
        int erabiltzaileId,
        int hilabeteKopurua,
        CancellationToken cancellationToken = default)
    {
        if (erabiltzaileId <= 0 || hilabeteKopurua <= 0)
            return Array.Empty<HilabetekoGastuAgregatua>();

        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var minHilabetea = SortuAzkenHilabeteenGakoak(hilabeteKopurua)[0];

        if (_urrunTursoModua)
        {
            var zerrenda = await ExekutatuTursoanAsync(async bezeroa =>
            {
                const string sql = """
                    SELECT substr(gl.GastuData, 1, 7) AS Hilabetea,
                           SUM(gl.Zenbatekoa_Guztira) AS Guztira
                    FROM GastuLerroak gl
                    INNER JOIN BidaiaTxostenak bt ON bt.TxostenId = gl.TxostenId
                    WHERE bt.ErabiltzaileId = ?
                      AND bt.Egoera = ?
                      AND length(gl.GastuData) >= 7
                      AND substr(gl.GastuData, 1, 7) >= ?
                    GROUP BY substr(gl.GastuData, 1, 7)
                    ORDER BY Hilabetea ASC;
                    """;
                var emaitza = await bezeroa.ExekutatuAsync(
                    sql,
                    cancellationToken,
                    LibsqlLoturaNormalizatua(erabiltzaileId),
                    LibsqlLoturaNormalizatua(TxostenEgoera.Onartua),
                    LibsqlLoturaNormalizatua(minHilabetea)).ConfigureAwait(false);
                return MapeatuHilabetekoGastuAgregatuak(emaitza);
            }, cancellationToken).ConfigureAwait(false);

            return OsatuHilabeteenZuloak(zerrenda, hilabeteKopurua);
        }

        var sqliteZerrenda = await _sqliteKonexioa!.QueryAsync<HilabetekoGastuAgregatua>(
            """
            SELECT substr(gl.GastuData, 1, 7) AS Hilabetea,
                   SUM(gl.Zenbatekoa_Guztira) AS Guztira
            FROM GastuLerroak gl
            INNER JOIN BidaiaTxostenak bt ON bt.TxostenId = gl.TxostenId
            WHERE bt.ErabiltzaileId = ?
              AND bt.Egoera = ?
              AND length(gl.GastuData) >= 7
              AND substr(gl.GastuData, 1, 7) >= ?
            GROUP BY substr(gl.GastuData, 1, 7)
            ORDER BY Hilabetea ASC
            """,
            erabiltzaileId,
            TxostenEgoera.Onartua,
            minHilabetea).ConfigureAwait(false);

        return OsatuHilabeteenZuloak(sqliteZerrenda, hilabeteKopurua);
    }

    public async Task<IReadOnlyList<HilabetekoGastuAgregatua>> EskuratuAzkenHilabeteetakoOnartutakoGastuakOrguOrokorraAsync(
        int hilabeteKopurua,
        CancellationToken cancellationToken = default)
    {
        if (hilabeteKopurua <= 0)
            return Array.Empty<HilabetekoGastuAgregatua>();

        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var minHilabetea = SortuAzkenHilabeteenGakoak(hilabeteKopurua)[0];

        if (_urrunTursoModua)
        {
            var zerrenda = await ExekutatuTursoanAsync(async bezeroa =>
            {
                const string sql = """
                    SELECT substr(gl.GastuData, 1, 7) AS Hilabetea,
                           SUM(gl.Zenbatekoa_Guztira) AS Guztira
                    FROM GastuLerroak gl
                    INNER JOIN BidaiaTxostenak bt ON bt.TxostenId = gl.TxostenId
                    WHERE bt.Egoera = ?
                      AND length(gl.GastuData) >= 7
                      AND substr(gl.GastuData, 1, 7) >= ?
                    GROUP BY substr(gl.GastuData, 1, 7)
                    ORDER BY Hilabetea ASC;
                    """;
                var emaitza = await bezeroa.ExekutatuAsync(
                    sql,
                    cancellationToken,
                    LibsqlLoturaNormalizatua(TxostenEgoera.Onartua),
                    LibsqlLoturaNormalizatua(minHilabetea)).ConfigureAwait(false);
                return MapeatuHilabetekoGastuAgregatuak(emaitza);
            }, cancellationToken).ConfigureAwait(false);

            return OsatuHilabeteenZuloak(zerrenda, hilabeteKopurua);
        }

        var sqliteZerrenda = await _sqliteKonexioa!.QueryAsync<HilabetekoGastuAgregatua>(
            """
            SELECT substr(gl.GastuData, 1, 7) AS Hilabetea,
                   SUM(gl.Zenbatekoa_Guztira) AS Guztira
            FROM GastuLerroak gl
            INNER JOIN BidaiaTxostenak bt ON bt.TxostenId = gl.TxostenId
            WHERE bt.Egoera = ?
              AND length(gl.GastuData) >= 7
              AND substr(gl.GastuData, 1, 7) >= ?
            GROUP BY substr(gl.GastuData, 1, 7)
            ORDER BY Hilabetea ASC
            """,
            TxostenEgoera.Onartua,
            minHilabetea).ConfigureAwait(false);

        return OsatuHilabeteenZuloak(sqliteZerrenda, hilabeteKopurua);
    }

    public async Task<IReadOnlyList<KategoriakoGastuAgregatua>> EskuratuKategoriakoOnartutakoGastuakLangileAsync(
        int erabiltzaileId,
        CancellationToken cancellationToken = default)
    {
        if (erabiltzaileId <= 0)
            return Array.Empty<KategoriakoGastuAgregatua>();

        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (_urrunTursoModua)
        {
            return await ExekutatuTursoanAsync(async bezeroa =>
            {
                const string sql = """
                    SELECT gk.Izena AS KontzeptuIzena,
                           SUM(gl.Zenbatekoa_Guztira) AS Guztira
                    FROM GastuLerroak gl
                    INNER JOIN BidaiaTxostenak bt ON bt.TxostenId = gl.TxostenId
                    INNER JOIN GastuKontzeptuak gk ON gk.KategoriaId = gl.KontzeptuId
                    WHERE bt.ErabiltzaileId = ?
                      AND bt.Egoera = ?
                    GROUP BY gk.Izena
                    ORDER BY Guztira DESC;
                    """;
                var emaitza = await bezeroa.ExekutatuAsync(
                    sql,
                    cancellationToken,
                    LibsqlLoturaNormalizatua(erabiltzaileId),
                    LibsqlLoturaNormalizatua(TxostenEgoera.Onartua)).ConfigureAwait(false);
                return MapeatuKategoriakoGastuAgregatuak(emaitza);
            }, cancellationToken).ConfigureAwait(false);
        }

        return await _sqliteKonexioa!.QueryAsync<KategoriakoGastuAgregatua>(
            """
            SELECT gk.Izena AS KontzeptuIzena,
                   SUM(gl.Zenbatekoa_Guztira) AS Guztira
            FROM GastuLerroak gl
            INNER JOIN BidaiaTxostenak bt ON bt.TxostenId = gl.TxostenId
            INNER JOIN GastuKontzeptuak gk ON gk.KategoriaId = gl.KontzeptuId
            WHERE bt.ErabiltzaileId = ?
              AND bt.Egoera = ?
            GROUP BY gk.Izena
            ORDER BY Guztira DESC
            """,
            erabiltzaileId,
            TxostenEgoera.Onartua).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<KategoriakoGastuAgregatua>> EskuratuKategoriakoOnartutakoGastuakOrguOrokorraAsync(
        CancellationToken cancellationToken = default)
    {
        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (_urrunTursoModua)
        {
            return await ExekutatuTursoanAsync(async bezeroa =>
            {
                const string sql = """
                    SELECT gk.Izena AS KontzeptuIzena,
                           SUM(gl.Zenbatekoa_Guztira) AS Guztira
                    FROM GastuLerroak gl
                    INNER JOIN BidaiaTxostenak bt ON bt.TxostenId = gl.TxostenId
                    INNER JOIN GastuKontzeptuak gk ON gk.KategoriaId = gl.KontzeptuId
                    WHERE bt.Egoera = ?
                    GROUP BY gk.Izena
                    ORDER BY Guztira DESC;
                    """;
                var emaitza = await bezeroa.ExekutatuAsync(
                    sql,
                    cancellationToken,
                    LibsqlLoturaNormalizatua(TxostenEgoera.Onartua)).ConfigureAwait(false);
                return MapeatuKategoriakoGastuAgregatuak(emaitza);
            }, cancellationToken).ConfigureAwait(false);
        }

        return await _sqliteKonexioa!.QueryAsync<KategoriakoGastuAgregatua>(
            """
            SELECT gk.Izena AS KontzeptuIzena,
                   SUM(gl.Zenbatekoa_Guztira) AS Guztira
            FROM GastuLerroak gl
            INNER JOIN BidaiaTxostenak bt ON bt.TxostenId = gl.TxostenId
            INNER JOIN GastuKontzeptuak gk ON gk.KategoriaId = gl.KontzeptuId
            WHERE bt.Egoera = ?
            GROUP BY gk.Izena
            ORDER BY Guztira DESC
            """,
            TxostenEgoera.Onartua).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TxostenEgoeraKopurua>> EskuratuTxostenKopuruakEgoerarenAraberaLangileAsync(
        int erabiltzaileId,
        CancellationToken cancellationToken = default)
    {
        if (erabiltzaileId <= 0)
            return Array.Empty<TxostenEgoeraKopurua>();

        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (_urrunTursoModua)
        {
            return await ExekutatuTursoanAsync(async bezeroa =>
            {
                const string sql = """
                    SELECT bt.Egoera AS Egoera, COUNT(*) AS Kopurua
                    FROM BidaiaTxostenak bt
                    WHERE bt.ErabiltzaileId = ?
                    GROUP BY bt.Egoera;
                    """;
                var emaitza = await bezeroa.ExekutatuAsync(
                    sql,
                    cancellationToken,
                    LibsqlLoturaNormalizatua(erabiltzaileId)).ConfigureAwait(false);
                return MapeatuTxostenEgoeraKopuruak(emaitza);
            }, cancellationToken).ConfigureAwait(false);
        }

        return await _sqliteKonexioa!.QueryAsync<TxostenEgoeraKopurua>(
            """
            SELECT bt.Egoera AS Egoera, COUNT(*) AS Kopurua
            FROM BidaiaTxostenak bt
            WHERE bt.ErabiltzaileId = ?
            GROUP BY bt.Egoera
            """,
            erabiltzaileId).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TxostenEgoeraKopurua>> EskuratuTxostenKopuruakEgoerarenAraberaOrguOrokorraAsync(
        CancellationToken cancellationToken = default)
    {
        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (_urrunTursoModua)
        {
            return await ExekutatuTursoanAsync(async bezeroa =>
            {
                const string sql = """
                    SELECT bt.Egoera AS Egoera, COUNT(*) AS Kopurua
                    FROM BidaiaTxostenak bt
                    GROUP BY bt.Egoera;
                    """;
                var emaitza = await bezeroa.ExekutatuAsync(sql, cancellationToken).ConfigureAwait(false);
                return MapeatuTxostenEgoeraKopuruak(emaitza);
            }, cancellationToken).ConfigureAwait(false);
        }

        return await _sqliteKonexioa!.QueryAsync<TxostenEgoeraKopurua>(
            """
            SELECT bt.Egoera AS Egoera, COUNT(*) AS Kopurua
            FROM BidaiaTxostenak bt
            GROUP BY bt.Egoera
            """).ConfigureAwait(false);
    }

    private async Task<double> EskuratuOnartutakoGastuenGuztiraBarneanAsync(
        int? erabiltzaileId,
        CancellationToken cancellationToken)
    {
        if (erabiltzaileId is <= 0)
            return 0;

        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (_urrunTursoModua)
        {
            return await ExekutatuTursoanAsync(async bezeroa =>
            {
                string sql;
                TursoHttpExekuzioarenEmaitza emaitza;
                if (erabiltzaileId is { } idLangile)
                {
                    sql = """
                          SELECT COALESCE(SUM(gl.Zenbatekoa_Guztira), 0) AS Guztira
                          FROM GastuLerroak gl
                          INNER JOIN BidaiaTxostenak bt ON bt.TxostenId = gl.TxostenId
                          WHERE bt.ErabiltzaileId = ?
                            AND bt.Egoera = ?;
                          """;
                    emaitza = await bezeroa.ExekutatuAsync(
                        sql,
                        cancellationToken,
                        LibsqlLoturaNormalizatua(idLangile),
                        LibsqlLoturaNormalizatua(TxostenEgoera.Onartua)).ConfigureAwait(false);
                }
                else
                {
                    sql = """
                          SELECT COALESCE(SUM(gl.Zenbatekoa_Guztira), 0) AS Guztira
                          FROM GastuLerroak gl
                          INNER JOIN BidaiaTxostenak bt ON bt.TxostenId = gl.TxostenId
                          WHERE bt.Egoera = ?;
                          """;
                    emaitza = await bezeroa.ExekutatuAsync(
                        sql,
                        cancellationToken,
                        LibsqlLoturaNormalizatua(TxostenEgoera.Onartua)).ConfigureAwait(false);
                }

                return IrakurriGuztiraLehena(emaitza);
            }, cancellationToken).ConfigureAwait(false);
        }

        if (erabiltzaileId is { } id)
        {
            var lerroak = await _sqliteKonexioa!.QueryAsync<GuztiraBatuka>(
                """
                SELECT COALESCE(SUM(gl.Zenbatekoa_Guztira), 0) AS Guztira
                FROM GastuLerroak gl
                INNER JOIN BidaiaTxostenak bt ON bt.TxostenId = gl.TxostenId
                WHERE bt.ErabiltzaileId = ?
                  AND bt.Egoera = ?
                """,
                id,
                TxostenEgoera.Onartua).ConfigureAwait(false);
            return lerroak.FirstOrDefault()?.Guztira ?? 0;
        }

        var guztiraOrgu = await _sqliteKonexioa!.QueryAsync<GuztiraBatuka>(
            """
            SELECT COALESCE(SUM(gl.Zenbatekoa_Guztira), 0) AS Guztira
            FROM GastuLerroak gl
            INNER JOIN BidaiaTxostenak bt ON bt.TxostenId = gl.TxostenId
            WHERE bt.Egoera = ?
            """,
            TxostenEgoera.Onartua).ConfigureAwait(false);

        return guztiraOrgu.FirstOrDefault()?.Guztira ?? 0;
    }

    private async Task<int> EskuratuZainTxartenKopuruaBarneanAsync(int? erabiltzaileId, CancellationToken cancellationToken)
    {
        if (erabiltzaileId is <= 0)
            return 0;

        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (_urrunTursoModua)
        {
            return await ExekutatuTursoanAsync(async bezeroa =>
            {
                TursoHttpExekuzioarenEmaitza emaitza;
                if (erabiltzaileId is { } idLangile)
                {
                    const string sql = """
                                       SELECT COUNT(*) AS Kopurua
                                       FROM BidaiaTxostenak bt
                                       WHERE bt.ErabiltzaileId = ?
                                         AND bt.Egoera = ?;
                                       """;
                    emaitza = await bezeroa.ExekutatuAsync(
                        sql,
                        cancellationToken,
                        LibsqlLoturaNormalizatua(idLangile),
                        LibsqlLoturaNormalizatua(TxostenEgoera.Zain)).ConfigureAwait(false);
                }
                else
                {
                    const string sql = """
                                       SELECT COUNT(*) AS Kopurua
                                       FROM BidaiaTxostenak bt
                                       WHERE bt.Egoera = ?;
                                       """;
                    emaitza = await bezeroa.ExekutatuAsync(
                        sql,
                        cancellationToken,
                        LibsqlLoturaNormalizatua(TxostenEgoera.Zain)).ConfigureAwait(false);
                }

                return IrakurriKopuruaLehena(emaitza);
            }, cancellationToken).ConfigureAwait(false);
        }

        if (erabiltzaileId is { } id)
        {
            var kop = await _sqliteKonexioa!.ExecuteScalarAsync<int>(
                """
                SELECT COUNT(*)
                FROM BidaiaTxostenak bt
                WHERE bt.ErabiltzaileId = ?
                  AND bt.Egoera = ?
                """,
                id,
                TxostenEgoera.Zain).ConfigureAwait(false);
            return kop;
        }

        return await _sqliteKonexioa!.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM BidaiaTxostenak bt
            WHERE bt.Egoera = ?
            """,
            TxostenEgoera.Zain).ConfigureAwait(false);
    }

    private static double IrakurriGuztiraLehena(TursoHttpExekuzioarenEmaitza emaitza)
    {
        var lerroa = emaitza.LerroTestuBalioak.FirstOrDefault();
        if (lerroa is null)
            return 0;

        var mapa = SortuTursoLerroMapa(emaitza.ZutabeIzenak, lerroa);
        return IrakurriMapaKomaHamarkatuaLehenetsia(mapa, "Guztira", 0);
    }

    private static int IrakurriKopuruaLehena(TursoHttpExekuzioarenEmaitza emaitza)
    {
        var lerroa = emaitza.LerroTestuBalioak.FirstOrDefault();
        if (lerroa is null)
            return 0;

        var mapa = SortuTursoLerroMapa(emaitza.ZutabeIzenak, lerroa);
        return IrakurriMapaOsoaLehenetsia(mapa, "Kopurua", 0);
    }

    private static IReadOnlyList<HilabetekoGastuAgregatua> MapeatuHilabetekoGastuAgregatuak(TursoHttpExekuzioarenEmaitza emaitza)
    {
        var zutabeak = emaitza.ZutabeIzenak;
        var zerrenda = new List<HilabetekoGastuAgregatua>();
        foreach (var lerroa in emaitza.LerroTestuBalioak)
        {
            var mapa = SortuTursoLerroMapa(zutabeak, lerroa);
            zerrenda.Add(new HilabetekoGastuAgregatua
            {
                Hilabetea = IrakurriMapaTestuaLehenetsia(mapa, "Hilabetea"),
                Guztira = IrakurriMapaKomaHamarkatuaLehenetsia(mapa, "Guztira", 0)
            });
        }

        return zerrenda;
    }

    private static IReadOnlyList<KategoriakoGastuAgregatua> MapeatuKategoriakoGastuAgregatuak(TursoHttpExekuzioarenEmaitza emaitza)
    {
        var zutabeak = emaitza.ZutabeIzenak;
        var zerrenda = new List<KategoriakoGastuAgregatua>();
        foreach (var lerroa in emaitza.LerroTestuBalioak)
        {
            var mapa = SortuTursoLerroMapa(zutabeak, lerroa);
            zerrenda.Add(new KategoriakoGastuAgregatua
            {
                KontzeptuIzena = IrakurriMapaTestuaLehenetsia(mapa, "KontzeptuIzena"),
                Guztira = IrakurriMapaKomaHamarkatuaLehenetsia(mapa, "Guztira", 0)
            });
        }

        return zerrenda;
    }

    private static IReadOnlyList<TxostenEgoeraKopurua> MapeatuTxostenEgoeraKopuruak(TursoHttpExekuzioarenEmaitza emaitza)
    {
        var zutabeak = emaitza.ZutabeIzenak;
        var zerrenda = new List<TxostenEgoeraKopurua>();
        foreach (var lerroa in emaitza.LerroTestuBalioak)
        {
            var mapa = SortuTursoLerroMapa(zutabeak, lerroa);
            zerrenda.Add(new TxostenEgoeraKopurua
            {
                Egoera = IrakurriMapaTestuaLehenetsia(mapa, "Egoera"),
                Kopurua = IrakurriMapaOsoaLehenetsia(mapa, "Kopurua", 0)
            });
        }

        return zerrenda;
    }

    private static IReadOnlyList<string> SortuAzkenHilabeteenGakoak(int hilabeteKopurua)
    {
        var zerrenda = new List<string>();
        var orain = DateTime.UtcNow;
        for (var i = hilabeteKopurua - 1; i >= 0; i--)
        {
            var d = orain.AddMonths(-i);
            zerrenda.Add($"{d.Year:D4}-{d.Month:D2}");
        }

        return zerrenda;
    }

    private static IReadOnlyList<HilabetekoGastuAgregatua> OsatuHilabeteenZuloak(
        IReadOnlyList<HilabetekoGastuAgregatua> datuak,
        int hilabeteKopurua)
    {
        var gakoak = SortuAzkenHilabeteenGakoak(hilabeteKopurua);
        var mapa = datuak.ToDictionary(x => x.Hilabetea, x => x.Guztira, StringComparer.Ordinal);
        return gakoak
            .Select(h => new HilabetekoGastuAgregatua { Hilabetea = h, Guztira = mapa.GetValueOrDefault(h, 0) })
            .ToList();
    }
}
