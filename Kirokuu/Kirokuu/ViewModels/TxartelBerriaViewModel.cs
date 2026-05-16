using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.Zerbitzuak;
using Kirokuu.ZerbitzuakSaioa;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Media;
using SQLite;
using System.IO;

namespace Kirokuu.ViewModels;

public partial class TxartelBerriaViewModel : ObservableObject
{
    private readonly DatuBaseaZerbitzua _datuBaseaZerbitzua;
    private readonly AutorizazioZerbitzua _autorizazioZerbitzua;
    private readonly ArgazkiIgotzeZerbitzua _argazkiIgotzeZerbitzua;
    private readonly ILogger<TxartelBerriaViewModel> _logger;
    private bool _zenbatekoaEguneratzen;

    private Dictionary<int, int> _ibilgailuaBeharDuKategoriaIdz = new();

    public static DateTime GaurkoData => DateTime.Today;

    public static IReadOnlyList<string> KategoriaIzenak { get; } = new[]
    {
        "Bazkaria",
        "Gasolina",
        "Garraio publikoa",
        "Hotela",
        "Peajea",
        "Aparkalekua",
        "Bidaia",
        "Materialak",
        "Bestelakoa"
    };

    public static IReadOnlyList<string> GarraioAukerenTestuak => GarraioBideaBalioak.AukeraEstadioak;

    public TxartelBerriaViewModel(
        DatuBaseaZerbitzua datuBaseaZerbitzua,
        AutorizazioZerbitzua autorizazioZerbitzua,
        ArgazkiIgotzeZerbitzua argazkiIgotzeZerbitzua,
        ILogger<TxartelBerriaViewModel> logger)
    {
        _datuBaseaZerbitzua = datuBaseaZerbitzua ?? throw new ArgumentNullException(nameof(datuBaseaZerbitzua));
        _autorizazioZerbitzua = autorizazioZerbitzua ?? throw new ArgumentNullException(nameof(autorizazioZerbitzua));
        _argazkiIgotzeZerbitzua = argazkiIgotzeZerbitzua ?? throw new ArgumentNullException(nameof(argazkiIgotzeZerbitzua));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [ObservableProperty]
    private bool _isKargatzean;

    [ObservableProperty]
    private string? _erroreMezua;

    [ObservableProperty]
    private int _hautatutakoKategoriaIndizea;

    [ObservableProperty]
    private string _zenbatekoaTestua = string.Empty;

    [ObservableProperty]
    private DateTime _hautatutakoData = DateTime.Today;

    [ObservableProperty]
    private string _deskribapena = string.Empty;

    [ObservableProperty]
    private string? _lokalArgazkiBidea;

    [ObservableProperty]
    private bool _argazkiHautatua;

    [ObservableProperty]
    private bool _ibilgailuaAukeraketaIkagarri;

    [ObservableProperty]
    private int _hautatutakoGarraioIndizea = -1;

    [ObservableProperty]
    private string _kilometroakTestua = string.Empty;

    [ObservableProperty]
    private bool _kilometroakIkagarri;

    [ObservableProperty]
    private string _sailarenEtiketa = string.Empty;

    partial void OnHautatutakoKategoriaIndizeaChanged(int value)
    {
        HautatutakoGarraioIndizea = -1;
        KilometroakIkagarri = false;
        KilometroakTestua = string.Empty;
        EguneratuIbilgailuaAukeraketaIkagarri();
    }

    partial void OnHautatutakoGarraioIndizeaChanged(int value)
    {
        if (!IbilgailuaAukeraketaIkagarri || value < 0 || value >= GarraioBideaBalioak.AukeraEstadioak.Count)
        {
            KilometroakIkagarri = false;
            return;
        }
        KilometroakIkagarri = GarraioBideaBalioak.IbilgailuaErabiltzenDu(GarraioBideaBalioak.AukeraEstadioak[value]);
        if (!KilometroakIkagarri)
            KilometroakTestua = string.Empty;
    }

    private void EguneratuIbilgailuaAukeraketaIkagarri()
    {
        var kategoriaId = HautatutakoKategoriaIndizea + 1;
        if (_ibilgailuaBeharDuKategoriaIdz.TryGetValue(kategoriaId, out var bandera))
            IbilgailuaAukeraketaIkagarri = bandera != 0;
        else
            IbilgailuaAukeraketaIkagarri = false;

        if (!IbilgailuaAukeraketaIkagarri)
        {
            KilometroakIkagarri = false;
            KilometroakTestua = string.Empty;
        }
    }

    partial void OnZenbatekoaTestuaChanged(string value)
    {
        if (_zenbatekoaEguneratzen || string.IsNullOrEmpty(value))
            return;

        var dezimala = value.Replace(',', '.');
        var garbia = new string(dezimala.Where(c => char.IsDigit(c) || c == '.').ToArray());

        var puntua = garbia.IndexOf('.');
        if (puntua >= 0 && garbia.Length - puntua - 1 > 2)
            garbia = garbia[..(puntua + 3)];

        if (garbia != value)
        {
            _zenbatekoaEguneratzen = true;
            ZenbatekoaTestua = garbia;
            _zenbatekoaEguneratzen = false;
        }
    }

    [RelayCommand]
    private async Task AgertzenDeneanAsync()
    {
        ErroreMezua = null;
        HautatutakoKategoriaIndizea = 0;
        ZenbatekoaTestua = string.Empty;
        HautatutakoData = DateTime.Today;
        Deskribapena = string.Empty;
        LokalArgazkiBidea = null;
        ArgazkiHautatua = false;
        HautatutakoGarraioIndizea = -1;
        IbilgailuaAukeraketaIkagarri = false;
        KilometroakTestua = string.Empty;
        KilometroakIkagarri = false;
        SailarenEtiketa = string.Empty;
        _ibilgailuaBeharDuKategoriaIdz = new Dictionary<int, int>();

        try
        {
            var erabiltzaileId = await _autorizazioZerbitzua.EskuratuOraingoErabiltzaileIdAsync().ConfigureAwait(true);
            if (erabiltzaileId is not null)
            {
                var erabiltzailea = await _datuBaseaZerbitzua.BilatuErabiltzaileaIdzAsync(erabiltzaileId.Value).ConfigureAwait(true);
                if (erabiltzailea is not null)
                {
                    var saila = SektoreaKargoarenHiztegia.LortuBaliozkotutakoSailaTestua(erabiltzailea.Sektorea);
                    SailarenEtiketa = string.IsNullOrEmpty(saila)
                        ? SektoreaKargoarenHiztegia.LortuSektorearenEtiketa(erabiltzailea.SektorearenIdentifikatzailea)
                        : saila;
                }
            }

            var kontzeptuak = await _datuBaseaZerbitzua.ZerrendatuGastuKontzeptuakAsync().ConfigureAwait(true);
            foreach (var k in kontzeptuak)
                _ibilgailuaBeharDuKategoriaIdz[k.KategoriaId] = k.IbilgailuaBeharDu;
            EguneratuIbilgailuaAukeraketaIkagarri();
        }
        catch (TursoExekuzioSalbuespena libEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                ?? "Datu-base errorea: ezin izan dira kategoriak kargatu.";
            _logger.LogError(libEx, "TxartelBerria: kategoriak kargatzean Turso errorea.");
        }
        catch (SQLiteException sqlEx)
        {
            ErroreMezua = "Datu-base errorea: ezin izan dira kategoriak kargatu.";
            _logger.LogError(sqlEx, "TxartelBerria: kategoriak kargatzean SQLite errorea.");
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Saiatu berriro.";
            _logger.LogError(ex, "TxartelBerria: kategoriak kargatzean errorea.");
        }
    }

    private const long ArgazkiGehienezkoTamainaBytes = 10 * 1024 * 1024; // 10 MB

    [RelayCommand]
    private async Task HautatuGaleriatikAsync()
    {
        try
        {
            if (!await EskatuGaleriaBaimenaAsync().ConfigureAwait(true))
            {
                ErroreMezua = "Galeria erabiltzeko baimena behar da. Ezarpenetan aktibatu.";
                return;
            }

            var argazkia = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Hautatu ticket argazkia"
            }).ConfigureAwait(true);

            await ProzesatuHautatutakoArgazkiaAsync(argazkia).ConfigureAwait(true);
        }
        catch (PermissionException)
        {
            ErroreMezua = "Baimena ukatu da. Ezarpenetan baimena eman.";
        }
        catch (FeatureNotSupportedException)
        {
            ErroreMezua = "Gailu honek ez du galeria onartzen.";
        }
        catch (Exception ex)
        {
            ErroreMezua = "Argazkia hautatzean errorea gertatu da.";
            _logger.LogError(ex, "HautatuGaleriatik: errorea.");
        }
    }

    [RelayCommand]
    private async Task AteraKameratikAsync()
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                ErroreMezua = "Gailu honek ez du kamerarik edo ez da onartzen.";
                return;
            }

            if (!await EskatuKameraBaimenaAsync().ConfigureAwait(true))
            {
                ErroreMezua = "Kamera erabiltzeko baimena behar da. Ezarpenetan aktibatu.";
                return;
            }

            var argazkia = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
            {
                Title = "Atera ticket argazkia"
            }).ConfigureAwait(true);

            await ProzesatuHautatutakoArgazkiaAsync(argazkia).ConfigureAwait(true);
        }
        catch (PermissionException)
        {
            ErroreMezua = "Baimena ukatu da. Ezarpenetan baimena eman.";
        }
        catch (FeatureNotSupportedException)
        {
            ErroreMezua = "Gailu honek ez du kamerarik.";
        }
        catch (Exception ex)
        {
            ErroreMezua = "Argazkia ateratzean errorea gertatu da.";
            _logger.LogError(ex, "AteraKameratik: errorea.");
        }
    }

    private static async Task<bool> EskatuKameraBaimenaAsync()
    {
        var egoera = await Permissions.CheckStatusAsync<Permissions.Camera>().ConfigureAwait(true);
        if (egoera == PermissionStatus.Granted)
            return true;

        egoera = await Permissions.RequestAsync<Permissions.Camera>().ConfigureAwait(true);
        return egoera == PermissionStatus.Granted;
    }

    private static async Task<bool> EskatuGaleriaBaimenaAsync()
    {
        if (DeviceInfo.Platform != DevicePlatform.Android)
            return true;

        PermissionStatus egoera;
        if (DeviceInfo.Version.Major >= 13)
        {
            egoera = await Permissions.CheckStatusAsync<Permissions.Photos>().ConfigureAwait(true);
            if (egoera == PermissionStatus.Granted)
                return true;

            egoera = await Permissions.RequestAsync<Permissions.Photos>().ConfigureAwait(true);
        }
        else
        {
            egoera = await Permissions.CheckStatusAsync<Permissions.StorageRead>().ConfigureAwait(true);
            if (egoera == PermissionStatus.Granted)
                return true;

            egoera = await Permissions.RequestAsync<Permissions.StorageRead>().ConfigureAwait(true);
        }

        return egoera == PermissionStatus.Granted;
    }

    [RelayCommand]
    private void KenduArgazkia()
    {
        LokalArgazkiBidea = null;
        ArgazkiHautatua = false;
        ErroreMezua = null;
    }

    private async Task ProzesatuHautatutakoArgazkiaAsync(FileResult? argazkia)
    {
        if (argazkia is null)
            return;

        if (string.IsNullOrWhiteSpace(argazkia.FullPath))
        {
            ErroreMezua = "Ezin izan da argazkiaren fitxategia irakurri. Saiatu berriro.";
            return;
        }

        if (!BalidatuArgazkiFitxategia(argazkia, out var balidazioMezua))
        {
            ErroreMezua = balidazioMezua;
            return;
        }

        ErroreMezua = null;
        LokalArgazkiBidea = argazkia.FullPath;
        ArgazkiHautatua = true;
        await Toast.Make("Argazkia prest dago. Gorde botoiarekin bidali dezakezu.").Show().ConfigureAwait(true);
    }

    private static bool BalidatuArgazkiFitxategia(FileResult argazkia, out string? erroreMezua)
    {
        var bidea = argazkia.FullPath;
        var izena = argazkia.FileName ?? Path.GetFileName(bidea);
        var luzapena = Path.GetExtension(izena);
        if (string.IsNullOrEmpty(luzapena))
            luzapena = ".jpg";

        luzapena = luzapena.ToLowerInvariant();
        if (luzapena is not ".jpg" and not ".jpeg" and not ".png")
        {
            erroreMezua = "Formatu onartua: JPEG edo PNG soilik.";
            return false;
        }

        if (!File.Exists(bidea))
        {
            erroreMezua = "Ezin izan da argazkiaren fitxategia irakurri. Saiatu berriro.";
            return false;
        }

        var tamaina = new FileInfo(bidea).Length;
        if (tamaina > ArgazkiGehienezkoTamainaBytes)
        {
            erroreMezua = "Argazkia handiegia da (gehienez 10 MB).";
            return false;
        }

        erroreMezua = null;
        return true;
    }

    [RelayCommand]
    private async Task GordeAsync()
    {
        ErroreMezua = null;

        if (string.IsNullOrWhiteSpace(ZenbatekoaTestua) ||
            !double.TryParse(ZenbatekoaTestua.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var zenbatekoa) ||
            zenbatekoa <= 0)
        {
            ErroreMezua = "Zenbatekoa baliogabea. Zenbaki positibo bat sartu.";
            return;
        }

        zenbatekoa = Math.Round(zenbatekoa, 2, MidpointRounding.AwayFromZero);

        var deskribapenaGarbia = Deskribapena.Trim();
        if (deskribapenaGarbia.Length < 3)
        {
            ErroreMezua = "Deskribapena gutxienez 3 karaktere izan behar ditu.";
            return;
        }

        if (HautatutakoData.Date > DateTime.Today)
        {
            ErroreMezua = "Ezin da etorkizuneko data bat aukeratu.";
            return;
        }

        if (IbilgailuaAukeraketaIkagarri)
        {
            var aukeraKopurua = GarraioBideaBalioak.AukeraEstadioak.Count;
            if (HautatutakoGarraioIndizea < 0 || HautatutakoGarraioIndizea >= aukeraKopurua)
            {
                ErroreMezua = "Hautatu garraio modua: enpresako ibilgailua, norberaren ibilgailua edo garraio publikoa.";
                return;
            }
        }

        try
        {
            IsKargatzean = true;

            if (await _autorizazioZerbitzua.DaZuzendariNagusiaAsync().ConfigureAwait(true))
                throw new UnauthorizedAccessException();

            var erabiltzaileId = await _autorizazioZerbitzua.EskuratuOraingoErabiltzaileIdAsync().ConfigureAwait(true);
            if (erabiltzaileId is null)
            {
                ErroreMezua = "Saioa iraungitu da. Berriz hasi saioa.";
                return;
            }

            var orain = DataOrduaBalioak.DataOrduaOrain();
            var dataTestua = DataOrduaBalioak.DataOrduaOsatuHautatutakoEguna(HautatutakoData);
            var kategoriaIzena = KategoriaIzenak[HautatutakoKategoriaIndizea];

            var erabiltzailea = await _datuBaseaZerbitzua.BilatuErabiltzaileaIdzAsync(erabiltzaileId.Value).ConfigureAwait(true);
            if (erabiltzailea is null)
            {
                ErroreMezua = "Ezin da erabiltzailearen DNI lortu. Berriz hasi saioa.";
                return;
            }
            var langileDni = erabiltzailea.DNI;

            var sailaGordetzeko = SektoreaKargoarenHiztegia.LortuBaliozkotutakoSailaTestua(erabiltzailea.Sektorea);
            if (string.IsNullOrEmpty(sailaGordetzeko))
            {
                ErroreMezua = "Zure saila ez dago ezarrita. Joan ezarpenetara eta hautatu Finantzak, Marketina edo Salmentak.";
                return;
            }

            string ticketArgazkiaUrl = string.Empty;
            if (!string.IsNullOrWhiteSpace(LokalArgazkiBidea))
            {
                try
                {
                    ticketArgazkiaUrl = await _argazkiIgotzeZerbitzua
                        .IgoArgazkiaAsync(LokalArgazkiBidea)
                        .ConfigureAwait(true);
                }
                catch (InvalidOperationException)
                {
                    ErroreMezua = "Argazkia igotzeko zerbitzua ez dago konfiguratua. Mesedez kendu argazkia eta berriro saiatu.";
                    return;
                }
                catch (HttpRequestException httpEx)
                {
                    ErroreMezua = "Argazkia igotzean sare errorea. Konexioa egiaztatu eta saiatu berriro.";
                    _logger.LogError(httpEx, "TxartelBerria: argazkia igotzean sare errorea.");
                    return;
                }
            }

            var garraioTestua = IbilgailuaAukeraketaIkagarri
                ? GarraioBideaBalioak.AukeraEstadioak[HautatutakoGarraioIndizea]
                : string.Empty;

            double kilometroak = 0;
            if (KilometroakIkagarri && !string.IsNullOrWhiteSpace(KilometroakTestua))
            {
                double.TryParse(KilometroakTestua.Replace(',', '.'),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out kilometroak);
            }

            var txostena = new BidaiaTxostena
            {
                ErabiltzaileId = erabiltzaileId.Value,
                LangileDNI = langileDni,
                Saila = sailaGordetzeko,
                Helmuga = kategoriaIzena,
                BidaiaHelburua = deskribapenaGarbia,
                HasieraData = dataTestua,
                AmaieraData = dataTestua,
                PertsonaKopurua = 1,
                JasoAurrekina = 0,
                Egoera = TxostenEgoera.Zain,
                AdminOharra = null,
                EmpresaIbilgailua = GarraioBideaBalioak.EnpresakoIbilgailuaBandera(garraioTestua),
                MonetaKodea = "EUR",
                SorkuntzaData = orain,
                AzkenEguneratzea = orain,
                DataAprobazioa = string.Empty
            };

            var gastuLerroa = new GastuLerroa
            {
                KategoriaId = HautatutakoKategoriaIndizea + 1,
                GastuData = dataTestua,
                GarraioBidea = garraioTestua,
                ZenbatekoaGuztira = zenbatekoa,
                Kilometroak = kilometroak,
                TicketArgazkia = ticketArgazkiaUrl,
                Oharrak = deskribapenaGarbia,
                KontzeptuId = HautatutakoKategoriaIndizea + 1,
                IbilgailuaBeharrezkoa = IbilgailuaAukeraketaIkagarri ? 1 : 0
            };

            await _datuBaseaZerbitzua
                .TxertatuBidaiaTxostenaEtaGastuLerroa(txostena, gastuLerroa)
                .ConfigureAwait(true);

            await Toast.Make("Gastua ondo gorde da.").Show().ConfigureAwait(true);
            await Shell.Current.GoToAsync("..").ConfigureAwait(true);
        }
        catch (TursoExekuzioSalbuespena libEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                ?? "Datu-base errorea: ezin izan da gorde. Saiatu berriro.";
            _logger.LogError(libEx, "TxartelBerria: Turso errorea.");
        }
        catch (SQLiteException sqlEx)
        {
            ErroreMezua = "Datu-base errorea: ezin izan da gorde. Saiatu berriro.";
            _logger.LogError(sqlEx, "TxartelBerria: SQLite errorea.");
        }
        catch (HttpRequestException httpEx)
        {
            ErroreMezua = "Sare errorea: konexioa egiaztatu eta saiatu berriro.";
            _logger.LogError(httpEx, "TxartelBerria: sare errorea.");
        }
        catch (TaskCanceledException)
        {
            ErroreMezua = "Eskaerak denbora muga gainditu du. Saiatu berriro.";
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "TxartelBerria: ustekabeko errorea.");
        }
        finally
        {
            IsKargatzean = false;
        }
    }

    [RelayCommand]
    private async Task EzeztatuAsync()
    {
        await Shell.Current.GoToAsync("..").ConfigureAwait(true);
    }
}
