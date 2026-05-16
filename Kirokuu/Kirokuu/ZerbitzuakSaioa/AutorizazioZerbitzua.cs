using System.Globalization;
using Kirokuu.DatuBasea.Ereduak;

namespace Kirokuu.ZerbitzuakSaioa;

public sealed class AutorizazioZerbitzua
{
    private readonly SaioaGordetzeZerbitzua _saioaGordetzeZerbitzua;
    private readonly DatuBaseaZerbitzua _datuBaseaZerbitzua;

    public AutorizazioZerbitzua(
        SaioaGordetzeZerbitzua saioaGordetzeZerbitzua,
        DatuBaseaZerbitzua datuBaseaZerbitzua)
    {
        _saioaGordetzeZerbitzua = saioaGordetzeZerbitzua ?? throw new ArgumentNullException(nameof(saioaGordetzeZerbitzua));
        _datuBaseaZerbitzua = datuBaseaZerbitzua ?? throw new ArgumentNullException(nameof(datuBaseaZerbitzua));
    }

    public async Task<int?> EskuratuOraingoErabiltzaileIdAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var testua = await _saioaGordetzeZerbitzua.IrakurriErabiltzaileIdTestuaAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(testua) ||
            !int.TryParse(testua, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            return null;

        return id;
    }

    public async Task<bool> DaAdministratzaileaAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rola = await EskuratuOraingoRolaAsync(cancellationToken).ConfigureAwait(false);
        return rola == (int)ErabiltzaileRola.Administratzailea;
    }

    public async Task<bool> DaZuzendariNagusiaAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rola = await EskuratuOraingoRolaAsync(cancellationToken).ConfigureAwait(false);
        return rola == (int)ErabiltzaileRola.ZuzendariNagusia;
    }

    public async Task<bool> DaNagusikoEstadistikaSarbideaAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rola = await EskuratuOraingoRolaAsync(cancellationToken).ConfigureAwait(false);
        return rola == (int)ErabiltzaileRola.Administratzailea ||
               rola == (int)ErabiltzaileRola.ZuzendariNagusia;
    }

    public async Task<int?> EskuratuAdminSektoreIragazkiaAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!await DaAdministratzaileaAsync(cancellationToken).ConfigureAwait(false))
            return null;

        var adminId = await EskuratuOraingoErabiltzaileIdAsync(cancellationToken).ConfigureAwait(false);
        if (adminId is not { } aid)
            return null;

        return await _datuBaseaZerbitzua
            .EskuratuAdministratzailearenSektoreIragazkiaAsync(aid, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<int?> EskuratuOraingoRolaAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rolaTestua = await _saioaGordetzeZerbitzua.IrakurriRolaTestuaAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(rolaTestua) ||
            !int.TryParse(rolaTestua, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rolaZenb))
            return null;

        return rolaZenb;
    }
}
