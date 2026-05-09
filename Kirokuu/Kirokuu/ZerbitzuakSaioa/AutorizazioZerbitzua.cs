using System.Globalization;
using Kirokuu.DatuBasea.Ereduak;

namespace Kirokuu.ZerbitzuakSaioa;

public sealed class AutorizazioZerbitzua
{
    private readonly SaioaGordetzeZerbitzua _saioaGordetzeZerbitzua;

    public AutorizazioZerbitzua(SaioaGordetzeZerbitzua saioaGordetzeZerbitzua)
    {
        _saioaGordetzeZerbitzua = saioaGordetzeZerbitzua ?? throw new ArgumentNullException(nameof(saioaGordetzeZerbitzua));
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
