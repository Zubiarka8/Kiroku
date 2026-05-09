namespace Kirokuu;

/// <summary>
/// FCM token berria gorde aurretik DI zerbitzuak eskuratzeko (Firebase gertaerak).
/// </summary>
public static class JakinarazpenMauiZerbitzuErreferentzia
{
    public static IServiceProvider? ZerbitzuHornitzailea { get; set; }
}
