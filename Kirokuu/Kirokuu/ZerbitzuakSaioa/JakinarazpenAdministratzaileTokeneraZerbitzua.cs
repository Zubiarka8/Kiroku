using Microsoft.Extensions.Logging;

namespace Kirokuu.ZerbitzuakSaioa;

/// <summary>
/// Administratzaile eta zuzendari nagusiarentzat FCM tokena datu-basean gordetzen du (Android).
/// </summary>
public sealed class JakinarazpenAdministratzaileTokeneraZerbitzua
{
    private readonly AutorizazioZerbitzua _autorizazioZerbitzua;
    private readonly DatuBaseaZerbitzua _datuBaseaZerbitzua;
    private readonly ILogger<JakinarazpenAdministratzaileTokeneraZerbitzua> _logger;

    public JakinarazpenAdministratzaileTokeneraZerbitzua(
        AutorizazioZerbitzua autorizazioZerbitzua,
        DatuBaseaZerbitzua datuBaseaZerbitzua,
        ILogger<JakinarazpenAdministratzaileTokeneraZerbitzua> logger)
    {
        _autorizazioZerbitzua = autorizazioZerbitzua ?? throw new ArgumentNullException(nameof(autorizazioZerbitzua));
        _datuBaseaZerbitzua = datuBaseaZerbitzua ?? throw new ArgumentNullException(nameof(datuBaseaZerbitzua));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SaiatuErregistratuAdministratzaileTaldeaAsync(
        CancellationToken cancellationToken = default)
    {
#if ANDROID
        cancellationToken.ThrowIfCancellationRequested();

        if (!await _autorizazioZerbitzua.DaNagusikoEstadistikaSarbideaAsync(cancellationToken).ConfigureAwait(false))
            return;

        var erabiltzaileId = await _autorizazioZerbitzua.EskuratuOraingoErabiltzaileIdAsync(cancellationToken).ConfigureAwait(false);
        if (erabiltzaileId is null)
            return;

        try
        {
            var baimena = await Microsoft.Maui.ApplicationModel.Permissions.RequestAsync<Microsoft.Maui.ApplicationModel.Permissions.PostNotifications>()
                .ConfigureAwait(false);
            if (baimena != Microsoft.Maui.ApplicationModel.PermissionStatus.Granted)
                _logger.LogWarning("Jakinarazpen baimena ez da eman.");

            var tokena = await Plugin.Firebase.CloudMessaging.CrossFirebaseCloudMessaging.Current.GetTokenAsync()
                .ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(tokena))
                return;

            await _datuBaseaZerbitzua
                .EguneratuJakinarazpenTokenaAsync(erabiltzaileId.Value, tokena, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException opEx)
        {
            _logger.LogError(opEx, "FCM tokena: Firebase ez dago prest.");
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("FCM tokena: denbora muga.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FCM tokena: errorea.");
        }
#else
        await Task.CompletedTask.ConfigureAwait(false);
#endif
    }
}
