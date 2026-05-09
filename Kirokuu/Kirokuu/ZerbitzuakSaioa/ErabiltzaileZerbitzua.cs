using System.Globalization;
using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.DatuEreduak;
using Microsoft.Extensions.Logging;
using SQLite;

namespace Kirokuu.ZerbitzuakSaioa;

public sealed class ErabiltzaileZerbitzua
{
    public const int Gehienezko = 5;

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
        string abizena2,
        string dni,
        string kargoa,
        string posta,
        string pasahitza,
        CancellationToken cancellationToken = default)
    {
        var pasahitzaGarbia = pasahitza.Trim();
        ArgumentException.ThrowIfNullOrWhiteSpace(pasahitzaGarbia);
        var (gatza, hash) = _pasahitzaZerbitzua.SortuGatzaEtaHash(pasahitzaGarbia);
        var orain = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        var erabiltzailea = new Erabiltzailea
        {
            Izena = izena.Trim(),
            Abizena = abizena.Trim(),
            Abizena2 = abizena2.Trim(),
            DNI = dni.Trim(),
            Posta = NormalizatuPosta(posta),
            Kargoa = kargoa.Trim(),
            SorkuntzaData = orain,
            Pasahitza = _pasahitzaZerbitzua.LotuGatzaEtaHashKatean(gatza, hash),
            Rola = (int)ErabiltzaileRola.Langilea,
            Aktiboa = 1
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
        var pasahitzaGarbia = pasahitza.Trim();
        if (string.IsNullOrEmpty(pasahitzaGarbia))
            return (SaioHasieraEmaitzaMota.PasahitzaOkerra, null);

        var erabiltzailea = await _datuBaseaZerbitzua.BilatuErabiltzaileaPostazAsync(postaNormalizatua, cancellationToken).ConfigureAwait(false);
        if (erabiltzailea is null)
            return (SaioHasieraEmaitzaMota.EzDaExistitzen, null);

        var zuzena = _pasahitzaZerbitzua.EgiaztatuGordetakoKatearekin(pasahitzaGarbia, erabiltzailea.Pasahitza);
        if (!zuzena)
            return (SaioHasieraEmaitzaMota.PasahitzaOkerra, null);

        if (erabiltzailea.Aktiboa == 0)
            return (SaioHasieraEmaitzaMota.KontuaDesaktibatuta, null);

        return (SaioHasieraEmaitzaMota.Ongi, erabiltzailea);
    }

    public async Task AdministratzaileakEguneratuErabiltzaileProfilaAsync(
        int erabiltzaileId,
        string izena,
        string abizena,
        string abizena2,
        string dni,
        string posta,
        string kargoa,
        int aktiboa,
        CancellationToken cancellationToken = default)
    {
        var erabiltzailea = await _datuBaseaZerbitzua.BilatuErabiltzaileaIdzAsync(erabiltzaileId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Erabiltzailea ez da aurkitu.");

        erabiltzailea.Izena = izena.Trim();
        erabiltzailea.Abizena = abizena.Trim();
        erabiltzailea.Abizena2 = abizena2.Trim();
        erabiltzailea.DNI = dni.Trim();
        erabiltzailea.Posta = NormalizatuPosta(posta);
        erabiltzailea.Kargoa = kargoa.Trim();
        erabiltzailea.Aktiboa = aktiboa == 0 ? 0 : 1;

        try
        {
            await _datuBaseaZerbitzua.EguneratuErabiltzaileaAsync(erabiltzailea, cancellationToken).ConfigureAwait(false);
        }
        catch (SQLiteException sqlEx) when (sqlEx.Result == SQLite3.Result.Constraint)
        {
            _logger.LogWarning(sqlEx, "Administratzaile eguneraketa: murrizketa.");
            throw;
        }
    }

    public async Task NorberarenProfilaEguneratuAsync(
        int erabiltzaileId,
        string izena,
        string abizena,
        string abizena2,
        string dni,
        string posta,
        string kargoa,
        CancellationToken cancellationToken = default)
    {
        var erabiltzailea = await _datuBaseaZerbitzua.BilatuErabiltzaileaIdzAsync(erabiltzaileId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Erabiltzailea ez da aurkitu.");

        erabiltzailea.Izena = izena.Trim();
        erabiltzailea.Abizena = abizena.Trim();
        erabiltzailea.Abizena2 = abizena2.Trim();
        erabiltzailea.DNI = dni.Trim();
        erabiltzailea.Posta = NormalizatuPosta(posta);
        erabiltzailea.Kargoa = kargoa.Trim();

        try
        {
            await _datuBaseaZerbitzua.EguneratuErabiltzaileaAsync(erabiltzailea, cancellationToken).ConfigureAwait(false);
        }
        catch (SQLiteException sqlEx) when (sqlEx.Result == SQLite3.Result.Constraint)
        {
            _logger.LogWarning(sqlEx, "Norberaren profila: murrizketa.");
            throw;
        }
    }

    public async Task AdministratzaileakBerrezarriPasahitzaLangilearentzatAsync(
        int erabiltzaileId,
        string pasahitzaBerria,
        CancellationToken cancellationToken = default)
    {
        var pasahitzaGarbia = pasahitzaBerria.Trim();
        ArgumentException.ThrowIfNullOrWhiteSpace(pasahitzaGarbia);

        var erabiltzailea = await _datuBaseaZerbitzua.BilatuErabiltzaileaIdzAsync(erabiltzaileId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Erabiltzailea ez da aurkitu.");

        var (gatza, hash) = _pasahitzaZerbitzua.SortuGatzaEtaHash(pasahitzaGarbia);
        erabiltzailea.Pasahitza = _pasahitzaZerbitzua.LotuGatzaEtaHashKatean(gatza, hash);
        await _datuBaseaZerbitzua.EguneratuErabiltzaileaAsync(erabiltzailea, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ErabiltzaileLaburpena?> EskuratuProfilLaburpenaIdzAsync(
        int erabiltzaileId,
        CancellationToken cancellationToken = default)
    {
        return await _datuBaseaZerbitzua.BilatuErabiltzaileLaburpenaIdzAsync(erabiltzaileId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Erabiltzailea?> EskuratuErabiltzaileaIdzAsync(
        int erabiltzaileId,
        CancellationToken cancellationToken = default)
    {
        return await _datuBaseaZerbitzua.BilatuErabiltzaileaIdzAsync(erabiltzaileId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> AldatuPasahitzaAsync(
        int erabiltzaileId,
        string pasahitzaZaharra,
        string pasahitzaBerria,
        CancellationToken cancellationToken = default)
    {
        var erabiltzailea = await _datuBaseaZerbitzua.BilatuErabiltzaileaIdzAsync(erabiltzaileId, cancellationToken).ConfigureAwait(false);
        if (erabiltzailea is null)
            return false;

        if (!_pasahitzaZerbitzua.EgiaztatuGordetakoKatearekin(pasahitzaZaharra.Trim(), erabiltzailea.Pasahitza))
            return false;

        var (gatza, hash) = _pasahitzaZerbitzua.SortuGatzaEtaHash(pasahitzaBerria.Trim());
        erabiltzailea.Pasahitza = _pasahitzaZerbitzua.LotuGatzaEtaHashKatean(gatza, hash);
        await _datuBaseaZerbitzua.EguneratuErabiltzaileaAsync(erabiltzailea, cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<IReadOnlyList<ErabiltzaileLaburpena>> EskuratuLangileenLaburpenakAsync(
        CancellationToken cancellationToken = default)
    {
        return await _datuBaseaZerbitzua.ZerrendatuLangileLaburpenakAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string NormalizatuPosta(string posta) => posta.Trim().ToLowerInvariant();
}
