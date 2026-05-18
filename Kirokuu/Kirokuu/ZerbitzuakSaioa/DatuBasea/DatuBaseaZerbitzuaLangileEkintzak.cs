using System.Globalization;
using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.DatuEreduak;
using Kirokuu.Zerbitzuak;

namespace Kirokuu.ZerbitzuakSaioa;

public sealed partial class DatuBaseaZerbitzua
{
    public async Task<IReadOnlyList<TxostenOnarpenLaburpena>> ZerrendatuLangilerenTxostenakAsync(
        int erabiltzaileId,
        string? egoeraIragazkia = null,
        CancellationToken cancellationToken = default)
    {
        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (_urrunTursoModua)
            return await ZerrendatuLangilerenTxostenakTursoAsync(erabiltzaileId, egoeraIragazkia, cancellationToken).ConfigureAwait(false);

        var iragazkia = !string.IsNullOrWhiteSpace(egoeraIragazkia);
        const string sqlOinarria = """
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
            WHERE b.ErabiltzaileId = ?
            """;

        if (iragazkia)
        {
            return await _sqliteKonexioa!.QueryAsync<TxostenOnarpenLaburpena>(
                sqlOinarria + " AND b.Egoera = ? ORDER BY b.SorkuntzaData DESC",
                erabiltzaileId, egoeraIragazkia!.Trim()).ConfigureAwait(false);
        }

        return await _sqliteKonexioa!.QueryAsync<TxostenOnarpenLaburpena>(
            sqlOinarria + " ORDER BY b.SorkuntzaData DESC",
            erabiltzaileId).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<TxostenOnarpenLaburpena>> ZerrendatuLangilerenTxostenakTursoAsync(
        int erabiltzaileId,
        string? egoeraIragazkia,
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
                  b.HasieraData AS HasieraData
                FROM BidaiaTxostenak b
                INNER JOIN Erabiltzaileak e ON e.ErabiltzaileId = b.ErabiltzaileId
                WHERE b.ErabiltzaileId = ?
                """;

            TursoHttpExekuzioarenEmaitza emaitza;
            if (!string.IsNullOrWhiteSpace(egoeraIragazkia))
            {
                emaitza = await bezeroa.ExekutatuAsync(
                    sqlOinarria + " AND b.Egoera = ? ORDER BY b.SorkuntzaData DESC;",
                    cancellationToken,
                    LibsqlLoturaNormalizatua(erabiltzaileId),
                    LibsqlLoturaNormalizatua(egoeraIragazkia.Trim())).ConfigureAwait(false);
            }
            else
            {
                emaitza = await bezeroa.ExekutatuAsync(
                    sqlOinarria + " ORDER BY b.SorkuntzaData DESC;",
                    cancellationToken,
                    LibsqlLoturaNormalizatua(erabiltzaileId)).ConfigureAwait(false);
            }
            return MapeatuTxostenOnarpenLaburrak(emaitza, null);
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task EzeztatuTxostenaLangileakAsync(int txostenId, int erabiltzaileId, CancellationToken cancellationToken = default)
    {
        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var orain = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

        if (_urrunTursoModua)
        {
            await ExekutatuTursoanAsync(async bezeroa =>
            {
                const string sql = """
                    UPDATE BidaiaTxostenak SET Egoera = ?, AzkenEguneraketa = ?
                    WHERE TxostenId = ? AND ErabiltzaileId = ? AND Egoera = ?;
                    """;
                await bezeroa.ExekutatuAsync(sql, cancellationToken,
                    LibsqlLoturaNormalizatua(TxostenEgoera.Ezeztatua),
                    LibsqlLoturaNormalizatua(orain),
                    LibsqlLoturaNormalizatua(txostenId),
                    LibsqlLoturaNormalizatua(erabiltzaileId),
                    LibsqlLoturaNormalizatua(TxostenEgoera.Zain)).ConfigureAwait(false);
                return 0;
            }, cancellationToken).ConfigureAwait(false);
            return;
        }

        await _sqliteKonexioa!.ExecuteAsync(
            "UPDATE BidaiaTxostenak SET Egoera = ?, AzkenEguneraketa = ? WHERE TxostenId = ? AND ErabiltzaileId = ? AND Egoera = ?",
            TxostenEgoera.Ezeztatua, orain, txostenId, erabiltzaileId, TxostenEgoera.Zain).ConfigureAwait(false);
    }

    public async Task TxertatuBidaiaTxostenaEtaGastuLerroa(
        BidaiaTxostena txostena,
        GastuLerroa gastuLerroa,
        CancellationToken cancellationToken = default)
    {
        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (_urrunTursoModua)
        {
            await ExekutatuTursoanAsync(async bezeroa =>
            {
                const string txostenSql = """
                    INSERT INTO BidaiaTxostenak
                      (ErabiltzaileId, LangileDNI, Saila, Helmuga, BidaiaHelburua,
                       HasieraData, AmaieraData, PertsonaKopurua, JasoAurrerakina, Egoera,
                       AdminOharra, MonetaKodea, SorkuntzaData, AzkenEguneraketa, DataAprobazioa)
                    VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?);
                    """;

                // last_insert_rowid() works within the same pipeline session (single HTTP request)
                const string gastuSql = """
                    INSERT INTO GastuLerroak
                      (TxostenId, KategoriaId, GastuData, GarraioBidea,
                       Zenbatekoa_Guztira, Kilometroak, TicketArgazkiBidea, Oharrak, KontzeptuId,
                       IbilgailuaBeharrezkoa)
                    VALUES (last_insert_rowid(), ?,?,?,?,?,?,?,?,?);
                    """;

                await bezeroa.ExekutatuBatchAsync(
                    new (string, object?[])[]
                    {
                        (txostenSql, new object?[]
                        {
                            LibsqlLoturaNormalizatua(txostena.ErabiltzaileId),
                            LibsqlLoturaNormalizatua(txostena.LangileDNI),
                            LibsqlLoturaNormalizatua(txostena.Saila),
                            LibsqlLoturaNormalizatua(txostena.Helmuga),
                            LibsqlLoturaNormalizatua(txostena.BidaiaHelburua),
                            LibsqlLoturaNormalizatua(txostena.HasieraData),
                            LibsqlLoturaNormalizatua(txostena.AmaieraData),
                            LibsqlLoturaNormalizatua(txostena.PertsonaKopurua),
                            LibsqlLoturaNormalizatua(txostena.JasoAurrerakina),
                            LibsqlLoturaNormalizatua(txostena.Egoera),
                            LibsqlLoturaNormalizatua(txostena.AdminOharra ?? string.Empty),
                            LibsqlLoturaNormalizatua(txostena.MonetaKodea),
                            LibsqlLoturaNormalizatua(txostena.SorkuntzaData),
                            LibsqlLoturaNormalizatua(txostena.AzkenEguneraketa),
                            LibsqlLoturaNormalizatua(txostena.DataAprobazioa)
                        }),
                        (gastuSql, new object?[]
                        {
                            LibsqlLoturaNormalizatua(gastuLerroa.KategoriaId),
                            LibsqlLoturaNormalizatua(gastuLerroa.GastuData),
                            LibsqlLoturaNormalizatua(gastuLerroa.GarraioBidea),
                            LibsqlLoturaNormalizatua(gastuLerroa.ZenbatekoaGuztira),
                            LibsqlLoturaNormalizatua(gastuLerroa.Kilometroak),
                            LibsqlLoturaNormalizatua(gastuLerroa.TicketArgazkia),
                            LibsqlLoturaNormalizatua(gastuLerroa.Oharrak),
                            LibsqlLoturaNormalizatua(gastuLerroa.KontzeptuId),
                            LibsqlLoturaNormalizatua(gastuLerroa.IbilgailuaBeharrezkoa)
                        })
                    },
                    cancellationToken).ConfigureAwait(false);

                return 0;
            }, cancellationToken).ConfigureAwait(false);
            await IdazkiAuditoretzaLogaAsync(AuditoretzaEkintzaTxostenaEskatuDu, txostena.Helmuga, txostena.ErabiltzaileId, cancellationToken).ConfigureAwait(false);
            return;
        }

        await _sqliteKonexioa!.InsertAsync(txostena).ConfigureAwait(false);
        gastuLerroa.TxostenId = txostena.TxostenId;
        await _sqliteKonexioa.InsertAsync(gastuLerroa).ConfigureAwait(false);
        await IdazkiAuditoretzaLogaAsync(AuditoretzaEkintzaTxostenaEskatuDu, $"{txostena.TxostenId} · {txostena.Helmuga}", txostena.ErabiltzaileId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<GastuKontzeptua>> ZerrendatuGastuKontzeptuakAsync(
        CancellationToken cancellationToken = default)
    {
        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (_urrunTursoModua)
        {
            return await ExekutatuTursoanAsync(async bezeroa =>
            {
                const string sql = """
                    SELECT * FROM GastuKontzeptuak ORDER BY KategoriaId;
                    """;
                var emaitza = await bezeroa.ExekutatuAsync(sql, cancellationToken).ConfigureAwait(false);
                return MapeatuGastuKontzeptuZerrenda(emaitza);
            }, cancellationToken).ConfigureAwait(false);
        }

        return await _sqliteKonexioa!.QueryAsync<GastuKontzeptua>(
            "SELECT * FROM GastuKontzeptuak ORDER BY KategoriaId").ConfigureAwait(false);
    }
}
