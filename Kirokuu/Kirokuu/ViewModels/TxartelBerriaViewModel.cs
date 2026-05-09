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

namespace Kirokuu.ViewModels;

public partial class TxartelBerriaViewModel : ObservableObject
{
    private readonly DatuBaseaZerbitzua _datuBaseaZerbitzua;
    private readonly AutorizazioZerbitzua _autorizazioZerbitzua;
    private readonly ArgazkiIgotzeZerbitzua _argazkiIgotzeZerbitzua;
    private readonly ILogger<TxartelBerriaViewModel> _logger;
    private bool _zenbatekoaEguneratzen;

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
    private void AgertzenDenean()
    {
        ErroreMezua = null;
        HautatutakoKategoriaIndizea = 0;
        ZenbatekoaTestua = string.Empty;
        HautatutakoData = DateTime.Today;
        Deskribapena = string.Empty;
        LokalArgazkiBidea = null;
        ArgazkiHautatua = false;
    }

    [RelayCommand]
    private async Task HautatuArgazkia()
    {
        try
        {
            var argazkia = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Hautatu ticket argazkia"
            }).ConfigureAwait(true);

            if (argazkia is null)
                return;

            LokalArgazkiBidea = argazkia.FullPath;
            ArgazkiHautatua = true;
        }
        catch (PermissionException)
        {
            ErroreMezua = "Baimena ukatu da. Ezarpenetan baimena eman.";
        }
        catch (Exception ex)
        {
            ErroreMezua = "Argazkia hautatzean errorea gertatu da.";
            _logger.LogError(ex, "HautatuArgazkia: errorea.");
        }
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

        if (string.IsNullOrWhiteSpace(Deskribapena))
        {
            ErroreMezua = "Deskribapena ezin da hutsik egon.";
            return;
        }

        try
        {
            IsKargatzean = true;

            var erabiltzaileId = await _autorizazioZerbitzua.EskuratuOraingoErabiltzaileIdAsync().ConfigureAwait(true);
            if (erabiltzaileId is null)
            {
                ErroreMezua = "Saioa iraungitu da. Berriz hasi saioa.";
                return;
            }

            var orain = DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture);
            var dataTestua = HautatutakoData.ToString("o", System.Globalization.CultureInfo.InvariantCulture);
            var kategoriaIzena = KategoriaIzenak[HautatutakoKategoriaIndizea];

            var erabiltzailea = await _datuBaseaZerbitzua.BilatuErabiltzaileaIdzAsync(erabiltzaileId.Value).ConfigureAwait(true);
            var langileDni = erabiltzailea?.DNI ?? string.Empty;

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

            var txostena = new BidaiaTxostena
            {
                ErabiltzaileId = erabiltzaileId.Value,
                LangileDNI = langileDni,
                Saila = string.Empty,
                Helmuga = kategoriaIzena,
                BidaiaHelburua = Deskribapena.Trim(),
                HasieraData = dataTestua,
                AmaieraData = dataTestua,
                PertsonaKopurua = 1,
                JasoAurrekina = 0,
                Egoera = TxostenEgoera.Zain,
                AdminOharra = null,
                MonetaKodea = "EUR",
                SorkuntzaData = orain,
                AzkenEguneratzea = orain,
                DataAprobazioa = string.Empty
            };

            var gastuLerroa = new GastuLerroa
            {
                KategoriaId = HautatutakoKategoriaIndizea + 1,
                GastuData = dataTestua,
                GarraioBidea = string.Empty,
                ZenbatekoaGuztira = zenbatekoa,
                Kilometroak = 0,
                TicketArgazkia = ticketArgazkiaUrl,
                Oharrak = Deskribapena.Trim(),
                KontzeptuId = HautatutakoKategoriaIndizea + 1
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
