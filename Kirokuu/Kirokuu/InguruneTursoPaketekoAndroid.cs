#if ANDROID
using Kirokuu.Zerbitzuak;
using Microsoft.Maui.Storage;

namespace Kirokuu;

internal static class InguruneTursoPaketekoAndroid
{
    /// <summary>
    /// Garapena (DEBUG): reposko .env-etik kopiatutako <c>turso_garapena.env</c> MauIAsset-etik kargatzen du.
    /// </summary>
    internal static bool KargatuTursoGarapenIngurunea()
    {
        try
        {
            using var fluxua = FileSystem.OpenAppPackageFileAsync("turso_garapena.env").GetAwaiter().GetResult();
            return InguruneKargatzailea.KargatuDotEnvFluxutik(fluxua);
        }
        catch (FileNotFoundException)
        {
            return false;
        }
    }
}
#endif
