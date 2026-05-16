using CommunityToolkit.Maui.Alerts;
using Kirokuu.Zerbitzuak;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Media;

namespace Kirokuu.ZerbitzuakSaioa;

public sealed class TxartelBerriaArgazkiLaguntzailea
{
    public async Task<(bool ongi, string? lokalBidea, string? erroreMezua)> HautatuGaleriatikAsync()
    {
        if (!await EskatuGaleriaBaimenaAsync().ConfigureAwait(false))
            return (false, null, "Galeria erabiltzeko baimena behar da. Ezarpenetan aktibatu.");

        var argazkia = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
        {
            Title = "Hautatu ticket argazkia"
        }).ConfigureAwait(false);

        return await ProzesatuHautatutakoArgazkiaAsync(argazkia).ConfigureAwait(false);
    }

    public async Task<(bool ongi, string? lokalBidea, string? erroreMezua)> AteraKameratikAsync()
    {
        if (!MediaPicker.Default.IsCaptureSupported)
            return (false, null, "Gailu honek ez du kamerarik edo ez da onartzen.");

        if (!await EskatuKameraBaimenaAsync().ConfigureAwait(false))
            return (false, null, "Kamera erabiltzeko baimena behar da. Ezarpenetan aktibatu.");

        var argazkia = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
        {
            Title = "Atera ticket argazkia"
        }).ConfigureAwait(false);

        return await ProzesatuHautatutakoArgazkiaAsync(argazkia).ConfigureAwait(false);
    }

    public static string? BalidatuFitxategia(string? fitxategiBidea) =>
        ArgazkiBalidazioLaguntzailea.EgiaztatuFitxategia(fitxategiBidea);

    private static async Task<(bool ongi, string? lokalBidea, string? erroreMezua)> ProzesatuHautatutakoArgazkiaAsync(FileResult? argazkia)
    {
        if (argazkia is null)
            return (false, null, null);

        if (string.IsNullOrWhiteSpace(argazkia.FullPath))
            return (false, null, "Ezin izan da argazkiaren fitxategia irakurri. Saiatu berriro.");

        var balidazioMezua = BalidatuFitxategia(argazkia.FullPath);
        if (balidazioMezua is not null)
            return (false, null, balidazioMezua);

        await Toast.Make("Argazkia prest dago. Gorde botoiarekin bidali dezakezu.").Show().ConfigureAwait(false);
        return (true, argazkia.FullPath, null);
    }

    private static async Task<bool> EskatuKameraBaimenaAsync()
    {
        var egoera = await Permissions.CheckStatusAsync<Permissions.Camera>().ConfigureAwait(false);
        if (egoera == PermissionStatus.Granted)
            return true;

        egoera = await Permissions.RequestAsync<Permissions.Camera>().ConfigureAwait(false);
        return egoera == PermissionStatus.Granted;
    }

    private static async Task<bool> EskatuGaleriaBaimenaAsync()
    {
        if (DeviceInfo.Platform != DevicePlatform.Android)
            return true;

        PermissionStatus egoera;
        if (DeviceInfo.Version.Major >= 13)
        {
            egoera = await Permissions.CheckStatusAsync<Permissions.Photos>().ConfigureAwait(false);
            if (egoera == PermissionStatus.Granted)
                return true;

            egoera = await Permissions.RequestAsync<Permissions.Photos>().ConfigureAwait(false);
        }
        else
        {
            egoera = await Permissions.CheckStatusAsync<Permissions.StorageRead>().ConfigureAwait(false);
            if (egoera == PermissionStatus.Granted)
                return true;

            egoera = await Permissions.RequestAsync<Permissions.StorageRead>().ConfigureAwait(false);
        }

        return egoera == PermissionStatus.Granted;
    }
}
