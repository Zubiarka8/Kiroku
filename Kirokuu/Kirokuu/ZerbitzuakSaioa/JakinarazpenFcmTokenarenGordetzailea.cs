using Kirokuu;
using Microsoft.Extensions.DependencyInjection;

namespace Kirokuu.ZerbitzuakSaioa;

/// <summary>
/// FCM token berria datu-basean gordetzen du administratzaile taldearentzat (Firebase TokenChanged).
/// </summary>
public static class JakinarazpenFcmTokenarenGordetzailea
{
    public static async Task SaiatuGordeAdministratzaileTokenaAsync(string? tokena)
    {
        if (string.IsNullOrWhiteSpace(tokena))
            return;

        var hornitzailea = JakinarazpenMauiZerbitzuErreferentzia.ZerbitzuHornitzailea;
        if (hornitzailea is null)
            return;

        try
        {
            var auth = hornitzailea.GetRequiredService<AutorizazioZerbitzua>();
            var db = hornitzailea.GetRequiredService<DatuBaseaZerbitzua>();

            if (!await auth.DaNagusikoEstadistikaSarbideaAsync().ConfigureAwait(false))
                return;

            var id = await auth.EskuratuOraingoErabiltzaileIdAsync().ConfigureAwait(false);
            if (id is null)
                return;

            await db.EguneratuJakinarazpenTokenaAsync(id.Value, tokena).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            // Zerbitzurik ez edo saiorik ez
        }
    }
}
