using Kirokuu.DatuBasea.Ereduak;

namespace Kirokuu.ZerbitzuakSaioa;

public sealed class SaioaGordetzeZerbitzua
{
    public const string GakoErabiltzaileId = "SaioErabiltzaileId";
    public const string GakoIzena = "SaioIzena";
    public const string GakoAbizena = "SaioAbizena";
    public const string GakoRola = "SaioRola";

    public async Task GordeAsync(Erabiltzailea erabiltzailea)
    {
        ArgumentNullException.ThrowIfNull(erabiltzailea);
        await SecureStorage.SetAsync(GakoErabiltzaileId, erabiltzailea.Id.ToString()).ConfigureAwait(false);
        await SecureStorage.SetAsync(GakoIzena, erabiltzailea.Izena).ConfigureAwait(false);
        await SecureStorage.SetAsync(GakoAbizena, erabiltzailea.Abizena).ConfigureAwait(false);
        await SecureStorage.SetAsync(GakoRola, erabiltzailea.Rola.ToString()).ConfigureAwait(false);
    }

    public async Task<bool> BadagoSaioaAsync()
    {
        var id = await SecureStorage.GetAsync(GakoErabiltzaileId).ConfigureAwait(false);
        return !string.IsNullOrWhiteSpace(id);
    }

    public async Task<(string Izena, string Abizena)?> IrakurriIzenAbizenakAsync()
    {
        var izena = await SecureStorage.GetAsync(GakoIzena).ConfigureAwait(false);
        var abizena = await SecureStorage.GetAsync(GakoAbizena).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(izena))
            return null;

        return (izena, abizena ?? string.Empty);
    }

    public async Task EguneratuIzenAbizenakSaioanAsync(string izena, string abizena)
    {
        await SecureStorage.SetAsync(GakoIzena, izena).ConfigureAwait(false);
        await SecureStorage.SetAsync(GakoAbizena, abizena).ConfigureAwait(false);
    }

    public Task<string?> IrakurriErabiltzaileIdTestuaAsync() =>
        SecureStorage.GetAsync(GakoErabiltzaileId);

    public Task<string?> IrakurriRolaTestuaAsync() =>
        SecureStorage.GetAsync(GakoRola);

    public Task GarbituAsync()
    {
        SecureStorage.Remove(GakoErabiltzaileId);
        SecureStorage.Remove(GakoIzena);
        SecureStorage.Remove(GakoAbizena);
        SecureStorage.Remove(GakoRola);
        return Task.CompletedTask;
    }
}
