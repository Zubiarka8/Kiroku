using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.AplikazioZerbitzuak;
using Kirokuu.Zerbitzuak;
using Kirokuu.ZerbitzuakSaioa;
using Microsoft.Extensions.Logging;
using SQLite;
using System.IO;

namespace Kirokuu.ViewModels;

public partial class TxartelBerriaViewModel : ObservableObject
{
    private readonly DatuBaseaZerbitzua _datuBaseaZerbitzua;
    private readonly AutorizazioZerbitzua _autorizazioZerbitzua;
    private readonly ArgazkiIgotzeZerbitzua _argazkiIgotzeZerbitzua;
    private readonly ILogger<TxartelBerriaViewModel> _logger;
    private readonly TxartelBerriaArgazkiLaguntzailea _argazkiLaguntzailea = new();
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
        catch (Exception ex)
        {
            ViewModelSalbuespenTratatzailea.TratatuIrakurketa(ex, m => ErroreMezua = m, _logger, "TxartelBerria kategoriak");
        }
    }


    [RelayCommand]
    private async Task HautatuGaleriatikAsync()
    {
        try
        {
            var (ongi, lokalBidea, errorea) = await _argazkiLaguntzailea.HautatuGaleriatikAsync().ConfigureAwait(true);
            if (!ongi)
            {
                if (errorea is not null)
                    ErroreMezua = errorea;
                return;
            }

            ErroreMezua = null;
            LokalArgazkiBidea = lokalBidea;
            ArgazkiHautatua = !string.IsNullOrWhiteSpace(lokalBidea);
        }
        catch (Exception ex)
        {
            if (!ViewModelSalbuespenTratatzailea.TratatuArgazkiEragiketa(ex, m => ErroreMezua = m, _logger, "HautatuGaleriatik", kamera: false))
                ErroreMezua = "Argazkia hautatzean errorea gertatu da.";
        }
    }

    [RelayCommand]
    private async Task AteraKameratikAsync()
    {
        try
        {
            var (ongi, lokalBidea, errorea) = await _argazkiLaguntzailea.AteraKameratikAsync().ConfigureAwait(true);
            if (!ongi)
            {
                if (errorea is not null)
                    ErroreMezua = errorea;
                return;
            }

            ErroreMezua = null;
            LokalArgazkiBidea = lokalBidea;
            ArgazkiHautatua = !string.IsNullOrWhiteSpace(lokalBidea);
        }
        catch (Exception ex)
        {
            if (!ViewModelSalbuespenTratatzailea.TratatuArgazkiEragiketa(ex, m => ErroreMezua = m, _logger, "AteraKameratik", kamera: true))
                ErroreMezua = "Argazkia ateratzean errorea gertatu da.";
        }
    }

    [RelayCommand]
    private void KenduArgazkia()
    {
        LokalArgazkiBidea = null;
        ArgazkiHautatua = false;
        ErroreMezua = null;
    }

    [RelayCommand]
    private async Task GordeAsync()
    {
        ErroreMezua = null;

        if (!ZenbatekoaBalidazioLaguntzailea.SaiatuParseatuDezimala(ZenbatekoaTestua, out var zenbatekoa) ||
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
                ZenbatekoaBalidazioLaguntzailea.SaiatuParseatuDezimala(KilometroakTestua, out kilometroak);
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
        catch (Exception ex)
        {
            ViewModelSalbuespenTratatzailea.TratatuIdazketa(ex, m => ErroreMezua = m, _logger, "TxartelBerria gorde");
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
