using System.Globalization;
using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.DatuEreduak;
using Kirokuu.Zerbitzuak;

namespace Kirokuu.ZerbitzuakSaioa;

public sealed partial class DatuBaseaZerbitzua
{
    public async Task<IReadOnlyList<TxostenOnarpenLaburpena>> ZerrendatuLangilerenTxostenakAsync(
        int erabiltzaileId,
        CancellationToken cancellationToken = default)
    {
        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (_urrunTursoModua)
            return await ZerrendatuLangilerenTxostenakTursoAsync(erabiltzaileId, cancellationToken).ConfigureAwait(false);

        return await _sqliteKonexioa!.QueryAsync<TxostenOnarpenLaburpena>(
            """
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
            ORDER BY b.SorkuntzaData DESC
            """,
            erabiltzaileId).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<TxostenOnarpenLaburpena>> ZerrendatuLangilerenTxostenakTursoAsync(
        int erabiltzaileId,
        CancellationToken cancellationToken)
    {
        return await ExekutatuTursoanAsync(async bezeroa =>
        {
            const string sql = """
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
                ORDER BY b.SorkuntzaData DESC;
                """;
            var emaitza = await bezeroa.ExekutatuAsync(sql, cancellationToken, LibsqlLoturaNormalizatua(erabiltzaileId)).ConfigureAwait(false);
            return MapeatuTxostenOnarpenLaburrak(emaitza, null);
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task EzeztatuTxostenaLangileakAsync(int txostenId, int erabiltzaileId, CancellationToken cancellationToken = default)
    {
        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (txostenId <= 0)
            throw new ArgumentException("Txosten ID baliogabea.", nameof(txostenId));
        if (erabiltzaileId <= 0)
            throw new ArgumentException("Erabiltzaile ID baliogabea.", nameof(erabiltzaileId));

        var orain = DataOrduaBalioak.DataOrduaOrain();
        int eragindakoErrenkadak;

        if (_urrunTursoModua)
        {
            eragindakoErrenkadak = await ExekutatuTursoanAsync(async bezeroa =>
            {
                const string sql = """
                    UPDATE BidaiaTxostenak SET Egoera = ?, AzkenEguneraketa = ?
                    WHERE TxostenId = ? AND ErabiltzaileId = ? AND Egoera = ?;
                    """;
                var emaitza = await bezeroa.ExekutatuAsync(sql, cancellationToken,
                    LibsqlLoturaNormalizatua(TxostenEgoera.Ezeztatua),
                    LibsqlLoturaNormalizatua(orain),
                    LibsqlLoturaNormalizatua(txostenId),
                    LibsqlLoturaNormalizatua(erabiltzaileId),
                    LibsqlLoturaNormalizatua(TxostenEgoera.Zain)).ConfigureAwait(false);
                return (int)emaitza.EragindakoErrenkadaKopurua;
            }, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            eragindakoErrenkadak = await _sqliteKonexioa!.ExecuteAsync(
                "UPDATE BidaiaTxostenak SET Egoera = ?, AzkenEguneraketa = ? WHERE TxostenId = ? AND ErabiltzaileId = ? AND Egoera = ?",
                TxostenEgoera.Ezeztatua, orain, txostenId, erabiltzaileId, TxostenEgoera.Zain).ConfigureAwait(false);
        }

        if (eragindakoErrenkadak <= 0)
            return;

        await IdatziAuditoretzaLogaAsync(
            AuditoretzaEkintzaTxostenaBertanBehera,
            $"{txostenId} · {TxostenEgoera.Ezeztatua}",
            erabiltzaileId,
            txostenId,
            erabiltzaileId,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task TxertatuBidaiaTxostenaEtaGastuLerroa(
        BidaiaTxostena txostena,
        GastuLerroa gastuLerroa,
        CancellationToken cancellationToken = default)
    {
        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        txostena.HasieraData = DataOrduaBalioak.DataOrduaOsatu(txostena.HasieraData);
        txostena.AmaieraData = DataOrduaBalioak.DataOrduaOsatu(txostena.AmaieraData);
        txostena.SorkuntzaData = DataOrduaBalioak.DataOrduaOsatu(txostena.SorkuntzaData);
        txostena.AzkenEguneratzea = DataOrduaBalioak.DataOrduaOsatu(txostena.AzkenEguneratzea);
        if (!string.IsNullOrWhiteSpace(txostena.DataAprobazioa))
            txostena.DataAprobazioa = DataOrduaBalioak.DataOrduaOsatu(txostena.DataAprobazioa);
        gastuLerroa.GastuData = DataOrduaBalioak.DataOrduaOsatu(gastuLerroa.GastuData);

        if (_urrunTursoModua)
        {
            var txostenIdBerria = await ExekutatuTursoanAsync(async bezeroa =>
            {
                const string txostenSql = """
                    INSERT INTO BidaiaTxostenak
                      (ErabiltzaileId, LangileDNI, Saila, Helmuga, BidaiaHelburua,
                       HasieraData, AmaieraData, PertsonaKopurua, JasoAurrerakina, Egoera,
                       AdminOharra, EmpresaIbilgailua, MonetaKodea, SorkuntzaData, AzkenEguneraketa, DataAprobazioa)
                    VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?);
                    """;

                // last_insert_rowid() works within the same pipeline session (single HTTP request)
                const string gastuSql = """
                    INSERT INTO GastuLerroak
                      (TxostenId, KategoriaId, GastuData, GarraioBidea,
                       Zenbatekoa_Guztira, Kilometroak, TicketArgazkiBidea, Oharrak, KontzeptuId, IbilgailuaBeharrezkoa)
                    VALUES (last_insert_rowid(), ?,?,?,?,?,?,?,?,?);
                    """;

                var emaitzak = await bezeroa.ExekutatuBatchAsync(
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
                            LibsqlLoturaNormalizatua(txostena.JasoAurrekina),
                            LibsqlLoturaNormalizatua(txostena.Egoera),
                            LibsqlLoturaNormalizatua(txostena.AdminOharra ?? string.Empty),
                            LibsqlLoturaNormalizatua(txostena.EmpresaIbilgailua),
                            LibsqlLoturaNormalizatua(txostena.MonetaKodea),
                            LibsqlLoturaNormalizatua(txostena.SorkuntzaData),
                            LibsqlLoturaNormalizatua(txostena.AzkenEguneratzea),
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

                return emaitzak.Count > 0 ? (int)emaitzak[0].AzkenTxertatutakoErrenkadaId : 0;
            }, cancellationToken).ConfigureAwait(false);

            var deskribapena = txostenIdBerria > 0
                ? $"{txostenIdBerria} · {txostena.Helmuga}"
                : txostena.Helmuga;
            await IdatziAuditoretzaLogaAsync(
                AuditoretzaEkintzaTxostenaEskatuDu,
                deskribapena,
                txostena.ErabiltzaileId,
                txostenIdBerria > 0 ? txostenIdBerria : null,
                txostena.ErabiltzaileId,
                cancellationToken).ConfigureAwait(false);
            return;
        }

        await _sqliteKonexioa!.InsertAsync(txostena).ConfigureAwait(false);
        gastuLerroa.TxostenId = txostena.TxostenId;
        await _sqliteKonexioa.InsertAsync(gastuLerroa).ConfigureAwait(false);
        await IdatziAuditoretzaLogaAsync(
            AuditoretzaEkintzaTxostenaEskatuDu,
            $"{txostena.TxostenId} · {txostena.Helmuga}",
            txostena.ErabiltzaileId,
            txostena.TxostenId,
            txostena.ErabiltzaileId,
            cancellationToken).ConfigureAwait(false);
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
