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
        await _datuBaseaZerbitzua.HasieratuAsync().ConfigureAwait(false);
        var konexioa = _datuBaseaZerbitzua.EskuratuKonexioa();
        var (gatza, hash) = _pasahitzaZerbitzua.SortuGatzaEtaHash(pasahitza);
        var erabiltzailea = new Erabiltzailea
        {
            Izena = izena.Trim(),
            Abizena = abizena.Trim(),
            Posta = NormalizatuPosta(posta),
            PasahitzaGatza = gatza,
            PasahitzaHash = hash,
            Rola = (int)ErabiltzaileRola.Langilea,
            HutsuneakSaioan = 0
        };

        try
        {
            await konexioa.InsertAsync(erabiltzailea).ConfigureAwait(false);
        }
        catch (SQLiteException sqlEx) when (sqlEx.Result == SQLite3.Result.Constraint)
        {
            _logger.LogWarning(sqlEx, "Erregistroa: posta bikoiztua.");
            throw;
        }

        var berrizIrakurria = (await konexioa
            .QueryAsync<Erabiltzailea>(
                "SELECT * FROM Erabiltzailea WHERE Posta = ? LIMIT 1",
                erabiltzailea.Posta)
            .ConfigureAwait(false)).FirstOrDefault();

        return berrizIrakurria ?? erabiltzailea;
    }

    public async Task<(SaioHasieraEmaitzaMota Mota, Erabiltzailea? Erabiltzailea)> SaioaHasiAsync(
        string posta,
        string pasahitza,
        CancellationToken cancellationToken = default)
    {
        await _datuBaseaZerbitzua.HasieratuAsync().ConfigureAwait(false);
        var konexioa = _datuBaseaZerbitzua.EskuratuKonexioa();
        var postaNormalizatua = NormalizatuPosta(posta);
        var zerrenda = await konexioa.QueryAsync<Erabiltzailea>(
            "SELECT * FROM Erabiltzailea WHERE Posta = ? LIMIT 1",
            postaNormalizatua).ConfigureAwait(false);

        var erabiltzailea = zerrenda.FirstOrDefault();
        if (erabiltzailea is null)
            return (SaioHasieraEmaitzaMota.EzDaExistitzen, null);

        if (erabiltzailea.HutsuneakSaioan >= GehienezkoHutsuneakSaioan)
            return (SaioHasieraEmaitzaMota.KontuaBlokeatuta, null);

        var zuzena = _pasahitzaZerbitzua.Egiaztatu(pasahitza, erabiltzailea.PasahitzaGatza, erabiltzailea.PasahitzaHash);
        if (!zuzena)
        {
            erabiltzailea.HutsuneakSaioan++;
            await konexioa.UpdateAsync(erabiltzailea).ConfigureAwait(false);
            return (SaioHasieraEmaitzaMota.PasahitzaOkerra, null);
        }

        erabiltzailea.HutsuneakSaioan = 0;
        await konexioa.UpdateAsync(erabiltzailea).ConfigureAwait(false);
        return (SaioHasieraEmaitzaMota.Ongi, erabiltzailea);
    }

    public async Task<ErabiltzaileLaburpena?> EskuratuProfilLaburpenaIdzAsync(
        int erabiltzaileId,
        CancellationToken cancellationToken = default)
    {
        await _datuBaseaZerbitzua.HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var konexioa = _datuBaseaZerbitzua.EskuratuKonexioa();
        var zerrenda = await konexioa.QueryAsync<ErabiltzaileLaburpena>(
            "SELECT Id, Izena, Abizena, Posta FROM Erabiltzailea WHERE Id = ? LIMIT 1",
            erabiltzaileId).ConfigureAwait(false);

        return zerrenda.FirstOrDefault();
    }

    public async Task<IReadOnlyList<ErabiltzaileLaburpena>> EskuratuLangileenLaburpenakAsync(
        CancellationToken cancellationToken = default)
    {
        await _datuBaseaZerbitzua.HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var konexioa = _datuBaseaZerbitzua.EskuratuKonexioa();
        var zerrenda = await konexioa.QueryAsync<ErabiltzaileLaburpena>(
            "SELECT Id, Izena, Abizena, Posta FROM Erabiltzailea WHERE Rola = ? ORDER BY Izena COLLATE NOCASE, Abizena COLLATE NOCASE",
            (int)ErabiltzaileRola.Langilea).ConfigureAwait(false);

        return zerrenda;
    }

    private static string NormalizatuPosta(string posta) => posta.Trim().ToLowerInvariant();
}
