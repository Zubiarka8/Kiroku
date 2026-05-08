using System.Runtime.InteropServices;
using CommunityToolkit.Maui.Alerts;
using Microsoft.Extensions.Logging;

namespace Kirokuu.ZerbitzuakSaioa;

public static class BokadilloErakustzailea
{
    private const int HresultElementuEzAurkitua = unchecked((int)0x80070490);

    public static async Task SaiatuErakutsiAsync(
        string testua,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(logger);

        try
        {
            await Toast.Make(testua).Show(cancellationToken).ConfigureAwait(true);
        }
        catch (COMException comEx) when (comEx.HResult == HresultElementuEzAurkitua)
        {
            logger.LogWarning(comEx, "Toast: Windows jakinarazpenen APIa ez dago eskuragarri (ERROR_NOT_FOUND).");
        }
        catch (COMException comEx)
        {
            logger.LogWarning(comEx, "Toast: COM errorea jakinarazpenak erakustean.");
        }
    }
}
