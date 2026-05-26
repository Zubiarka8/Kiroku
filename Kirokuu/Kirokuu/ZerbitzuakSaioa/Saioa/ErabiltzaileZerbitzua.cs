using System.Globalization;
using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.DatuEreduak;
using Kirokuu.Zerbitzuak;
using Microsoft.Extensions.Logging;
using SQLite;

namespace Kirokuu.ZerbitzuakSaioa;

public sealed class ErabiltzaileZerbitzua
{
    public const int HutsSaioSaiakeraGehienezkoa = 5;

    public const int SaioBlokeoaIraupenaMinutuak = 15;

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
        string sektoreIzena,
        int kargoarenIdentifikatzailea,
        string posta,
        string pasahitza,
        CancellationToken cancellationToken = default)
    {
        var pasahitzaGarbia = pasahitza.Trim();
        ArgumentException.ThrowIfNullOrWhiteSpace(pasahitzaGarbia);
        var (gatza, hash) = _pasahitzaZerbitzua.SortuGatzaEtaHash(pasahitzaGarbia);
        var orain = DataOrduaBalioak.DataOrduaOrain();
        var erabiltzailea = new Erabiltzailea
        {
            Izena = izena.Trim(),
            Abizena = abizena.Trim(),
            Abizena2 = abizena2.Trim(),
            DNI = dni.Trim(),
            Posta = NormalizatuPosta(posta),
            SorkuntzaData = orain,
            Pasahitza = _pasahitzaZerbitzua.LotuGatzaEtaHashKatean(gatza, hash),
            Rola = (int)ErabiltzaileRola.Langilea,
            Aktiboa = 1
        };
        EzarriSektoreaEtaKargoarenBalioak(erabiltzailea, sektoreIzena, kargoarenIdentifikatzailea);
        erabiltzailea.SaioHasieraSaiakerak = 0;
        erabiltzailea.SaioaBlokeoaAmaieraUtc = null;

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

    public async Task<(SaioHasieraEmaitzaMota Mota, Erabiltzailea? Erabiltzailea, TimeSpan? SaioBlokeoaGeratzen)> SaioaHasiAsync(
        string posta,
        string pasahitza,
        CancellationToken cancellationToken = default)
    {
        var postaNormalizatua = NormalizatuPosta(posta);
        var pasahitzaGarbia = pasahitza.Trim();
        if (string.IsNullOrEmpty(pasahitzaGarbia))
            return (SaioHasieraEmaitzaMota.PasahitzaOkerra, null, null);

        var erabiltzailea = await _datuBaseaZerbitzua.BilatuErabiltzaileaPostazAsync(postaNormalizatua, cancellationToken).ConfigureAwait(false);
        if (erabiltzailea is null)
            return (SaioHasieraEmaitzaMota.EzDaExistitzen, null, null);

        var orain = DateTime.UtcNow;
        if (SaiatuIrakurriBlokeoaAmaieraUtc(erabiltzailea.SaioaBlokeoaAmaieraUtc, out var blokeoaAmaieraUtc))
        {
            if (orain >= blokeoaAmaieraUtc)
            {
                erabiltzailea.SaioaBlokeoaAmaieraUtc = null;
                erabiltzailea.SaioHasieraSaiakerak = 0;
                await _datuBaseaZerbitzua.EguneratuErabiltzaileaAsync(erabiltzailea, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var geratzen = blokeoaAmaieraUtc - orain;
                return (SaioHasieraEmaitzaMota.SaioDenborazBlokeatuta, null,
                    geratzen > TimeSpan.Zero ? geratzen : TimeSpan.Zero);
            }
        }

        var zuzena = _pasahitzaZerbitzua.EgiaztatuGordetakoKatearekin(pasahitzaGarbia, erabiltzailea.Pasahitza);
        if (!zuzena)
        {
            erabiltzailea.SaioHasieraSaiakerak++;
            if (erabiltzailea.SaioHasieraSaiakerak >= HutsSaioSaiakeraGehienezkoa)
            {
                var amaiera = orain.AddMinutes(SaioBlokeoaIraupenaMinutuak);
                erabiltzailea.SaioaBlokeoaAmaieraUtc = amaiera.ToString("o", CultureInfo.InvariantCulture);
                erabiltzailea.SaioHasieraSaiakerak = 0;
                await _datuBaseaZerbitzua.EguneratuErabiltzaileaAsync(erabiltzailea, cancellationToken).ConfigureAwait(false);
                return (SaioHasieraEmaitzaMota.SaioDenborazBlokeatuta, null,
                    TimeSpan.FromMinutes(SaioBlokeoaIraupenaMinutuak));
            }

            await _datuBaseaZerbitzua.EguneratuErabiltzaileaAsync(erabiltzailea, cancellationToken).ConfigureAwait(false);
            return (SaioHasieraEmaitzaMota.PasahitzaOkerra, null, null);
        }

        erabiltzailea.SaioHasieraSaiakerak = 0;
        erabiltzailea.SaioaBlokeoaAmaieraUtc = null;
        await _datuBaseaZerbitzua.EguneratuErabiltzaileaAsync(erabiltzailea, cancellationToken).ConfigureAwait(false);

        if (erabiltzailea.Aktiboa == 0)
            return (SaioHasieraEmaitzaMota.KontuaDesaktibatuta, null, null);

        return (SaioHasieraEmaitzaMota.Ongi, erabiltzailea, null);
    }

    private static bool SaiatuIrakurriBlokeoaAmaieraUtc(string? iso, out DateTime amaieraUtc)
    {
        amaieraUtc = default;
        if (string.IsNullOrWhiteSpace(iso))
            return false;

        if (!DateTime.TryParse(
                iso,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces,
                out var data))
            return false;

        amaieraUtc = data.Kind switch
        {
            DateTimeKind.Utc => data,
            DateTimeKind.Local => data.ToUniversalTime(),
            _ => DateTime.SpecifyKind(data, DateTimeKind.Utc)
        };
        return true;
    }

    public async Task AdministratzaileakEguneratuErabiltzaileProfilaAsync(
        int erabiltzaileId,
        string izena,
        string abizena,
        string abizena2,
        string dni,
        string posta,
        string sektoreIzena,
        int kargoarenIdentifikatzailea,
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
        EzarriSektoreaEtaKargoarenBalioak(erabiltzailea, sektoreIzena, kargoarenIdentifikatzailea);
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
        string sektoreIzena,
        int kargoarenIdentifikatzailea,
        CancellationToken cancellationToken = default)
    {
        var erabiltzailea = await _datuBaseaZerbitzua.BilatuErabiltzaileaIdzAsync(erabiltzaileId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Erabiltzailea ez da aurkitu.");

        erabiltzailea.Izena = izena.Trim();
        erabiltzailea.Abizena = abizena.Trim();
        erabiltzailea.Abizena2 = abizena2.Trim();
        erabiltzailea.DNI = dni.Trim();
        erabiltzailea.Posta = NormalizatuPosta(posta);
        EzarriSektoreaEtaKargoarenBalioak(erabiltzailea, sektoreIzena, kargoarenIdentifikatzailea);

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
        erabiltzailea.SaioHasieraSaiakerak = 0;
        erabiltzailea.SaioaBlokeoaAmaieraUtc = null;
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
        erabiltzailea.SaioHasieraSaiakerak = 0;
        erabiltzailea.SaioaBlokeoaAmaieraUtc = null;
        await _datuBaseaZerbitzua.EguneratuErabiltzaileaAsync(erabiltzailea, cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> NANErabilitaDagoaAsync(string nan, int? ezezErabiltzaileId = null, CancellationToken cancellationToken = default)
    {
        return await _datuBaseaZerbitzua.NANErabilitaDagoaAsync(nan, ezezErabiltzaileId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ErabiltzaileLaburpena>> EskuratuLangileenLaburpenakAsync(
        int? sektoreIragazkia = null,
        bool soilikLangileak = true,
        CancellationToken cancellationToken = default)
    {
        return await _datuBaseaZerbitzua
            .ZerrendatuLangileLaburpenakAsync(sektoreIragazkia, soilikLangileak, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task EguneratuErabiltzaileaAktiboaAsync(
        int erabiltzaileId,
        int aktiboa,
        CancellationToken cancellationToken = default)
    {
        var erabiltzailea = await _datuBaseaZerbitzua.BilatuErabiltzaileaIdzAsync(erabiltzaileId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Erabiltzailea ez da aurkitu.");

        erabiltzailea.Aktiboa = aktiboa == 0 ? 0 : 1;

        try
        {
            await _datuBaseaZerbitzua.EguneratuErabiltzaileaAsync(erabiltzailea, cancellationToken).ConfigureAwait(false);
        }
        catch (SQLiteException sqlEx) when (sqlEx.Result == SQLite3.Result.Constraint)
        {
            _logger.LogWarning(sqlEx, "Aktiboa eguneraketa: murrizketa.");
            throw;
        }
    }

    public async Task<IReadOnlyList<ErabiltzaileLaburpena>> EskuratuLangileenLaburpenakAdministratzailearentzatAsync(
        int administratzaileErabiltzaileId,
        CancellationToken cancellationToken = default)
    {
        var sektoreIragazkia = await _datuBaseaZerbitzua
            .EskuratuAdministratzailearenSektoreIragazkiaAsync(administratzaileErabiltzaileId, cancellationToken)
            .ConfigureAwait(false);
        return await EskuratuLangileenLaburpenakAsync(sektoreIragazkia, soilikLangileak: true, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> AdministratzaileakErabiltzaileaIkusiDezakeAsync(
        int administratzaileErabiltzaileId,
        int erabiltzaileId,
        CancellationToken cancellationToken = default)
    {
        var sektoreIragazkia = await _datuBaseaZerbitzua
            .EskuratuAdministratzailearenSektoreIragazkiaAsync(administratzaileErabiltzaileId, cancellationToken)
            .ConfigureAwait(false);
        if (sektoreIragazkia is null)
            return true;

        return await _datuBaseaZerbitzua
            .ErabiltzaileaSektorearekinBatDatorAsync(erabiltzaileId, sektoreIragazkia.Value, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> AdministratzaileakSektoreaKudeatuDezakeAsync(
        int administratzaileErabiltzaileId,
        int sektorearenIdentifikatzailea,
        CancellationToken cancellationToken = default)
    {
        var sektoreIragazkia = await _datuBaseaZerbitzua
            .EskuratuAdministratzailearenSektoreIragazkiaAsync(administratzaileErabiltzaileId, cancellationToken)
            .ConfigureAwait(false);
        if (sektoreIragazkia is null)
            return true;

        return sektoreIragazkia.Value == sektorearenIdentifikatzailea;
    }

    private static string NormalizatuPosta(string posta) => posta.Trim().ToLowerInvariant();

    private static void EzarriSektoreaEtaKargoarenBalioak(
        Erabiltzailea erabiltzailea,
        string sektoreIzena,
        int kargoarenIdentifikatzailea)
    {
        erabiltzailea.Sektorea = sektoreIzena?.Trim() ?? string.Empty;
        erabiltzailea.Kargoa = SektoreaKargoarenHiztegia.LortuKargoarenEtiketa((EnpresakoLangileKargoa)kargoarenIdentifikatzailea);
    }
}
