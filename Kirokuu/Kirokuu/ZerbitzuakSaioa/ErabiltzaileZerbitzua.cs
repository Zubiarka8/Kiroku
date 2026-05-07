using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.DatuEreduak;
using Microsoft.Extensions.Logging;
using SQLite;

namespace Kirokuu.ZerbitzuakSaioa;

public sealed class ErabiltzaileZerbitzua
{
    public const int GehienezkoHutsuneakSaioan = 5;

    private readonly DatuBaseaZerbitzua _datuBaseaZerbitzua;
    private readonly PasahitzaZerbitzua _pasahitzaZerbitzua;
    private readonly ILogger<ErabiltzaileZerbitzua> _logger;

    public ErabiltzaileZerbitzua(
        DatuBaseaZerbitzua datuBaseaZerbitzua,
        PasahitzaZerbitzua pasahitzaZerbitzua,
        ILogger<ErabiltzaileZerbitzua> logger)
    {
        _datuBaseaZerbitzua = datuBaseaZerbitzua ?? throw new ArgumentNullException(nameof(datuBaseaZerbitzua));
        _pasahitzaZerbitzua = pasahitzaZerbitzua ?? throw new ArgumentNullException(nameof(pasahitzaZerbitzua));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Erabiltzailea> ErregistratuLangileaAsync(
        string izena,
        string abizena,
        string posta,
        string pasahitza,
        CancellationToken cancellationToken = default)
    {
        var (gatza, hash) = _pasahitzaZerbitzua.SortuGatzaEtaHash(pasahitza);
        var orain = DateTime.UtcNow;
        var erabiltzailea = new Erabiltzailea
        {
            Izena = izena.Trim(),
            Abizena = abizena.Trim(),
            Abizena2 = string.Empty,
            Nan = string.Empty,
            Posta = NormalizatuPosta(posta),
            Kargoa = string.Empty,
            SorkuntzaData = orain,
            PasahitzaGatza = gatza,
            PasahitzaHash = hash,
            Rola = (int)ErabiltzaileRola.Langilea,
            HutsuneakSaioan = 0
        };

        try
        {
            return await _datuBaseaZerbitzua.TxertatuErabiltzaileaAsync(erabiltzailea, cancellationToken).ConfigureAwait(false);
        }
        catch (SQLiteException sqlEx) when (sqlEx.Result == SQLite3.Result.Constraint)
        {
            _logger.LogWarning(sqlEx, "Erregistroa: posta bikoiztua.");
            throw;
        }
    }

    public async Task<(SaioHasieraEmaitzaMota Mota, Erabiltzailea? Erabiltzailea)> SaioaHasiAsync(
        string posta,
        string pasahitza,
        CancellationToken cancellationToken = default)
    {
        var postaNormalizatua = NormalizatuPosta(posta);
        var erabiltzailea = await _datuBaseaZerbitzua.BilatuErabiltzaileaPostazAsync(postaNormalizatua, cancellationToken).ConfigureAwait(false);
        if (erabiltzailea is null)
            return (SaioHasieraEmaitzaMota.EzDaExistitzen, null);

        if (erabiltzailea.HutsuneakSaioan >= GehienezkoHutsuneakSaioan)
            return (SaioHasieraEmaitzaMota.KontuaBlokeatuta, null);

        var zuzena = _pasahitzaZerbitzua.Egiaztatu(pasahitza, erabiltzailea.PasahitzaGatza, erabiltzailea.PasahitzaHash);
        if (!zuzena)
        {
            erabiltzailea.HutsuneakSaioan++;
            await _datuBaseaZerbitzua.EguneratuErabiltzaileaAsync(erabiltzailea, cancellationToken).ConfigureAwait(false);
            return (SaioHasieraEmaitzaMota.PasahitzaOkerra, null);
        }

        erabiltzailea.HutsuneakSaioan = 0;
        await _datuBaseaZerbitzua.EguneratuErabiltzaileaAsync(erabiltzailea, cancellationToken).ConfigureAwait(false);
        return (SaioHasieraEmaitzaMota.Ongi, erabiltzailea);
    }

    public async Task<ErabiltzaileLaburpena?> EskuratuProfilLaburpenaIdzAsync(
        int erabiltzaileId,
        CancellationToken cancellationToken = default)
    {
        return await _datuBaseaZerbitzua.BilatuErabiltzaileLaburpenaIdzAsync(erabiltzaileId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ErabiltzaileLaburpena>> EskuratuLangileenLaburpenakAsync(
        CancellationToken cancellationToken = default)
    {
        return await _datuBaseaZerbitzua.ZerrendatuLangileLaburpenakAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string NormalizatuPosta(string posta) => posta.Trim().ToLowerInvariant();
}
