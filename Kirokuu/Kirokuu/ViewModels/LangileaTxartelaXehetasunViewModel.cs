using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.Zerbitzuak;
using Kirokuu.ZerbitzuakSaioa;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using SQLite;

namespace Kirokuu.ViewModels;

[QueryProperty(nameof(TxostenIdQuery), "TxostenId")]
public partial class LangileaTxartelaXehetasunViewModel : ObservableObject
{
    private readonly DatuBaseaZerbitzua _datuBaseaZerbitzua;
    private readonly AutorizazioZerbitzua _autorizazioZerbitzua;
    private readonly ILogger<LangileaTxartelaXehetasunViewModel> _logger;

    private int _txostenIdGordeta;

    public LangileaTxartelaXehetasunViewModel(
        DatuBaseaZerbitzua datuBaseaZerbitzua,
        AutorizazioZerbitzua autorizazioZerbitzua,
        ILogger<LangileaTxartelaXehetasunViewModel> logger)
    {
        _datuBaseaZerbitzua = datuBaseaZerbitzua ?? throw new ArgumentNullException(nameof(datuBaseaZerbitzua));
        _autorizazioZerbitzua = autorizazioZerbitzua ?? throw new ArgumentNullException(nameof(autorizazioZerbitzua));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [ObservableProperty]
    private string _txostenIdQuery = string.Empty;

    partial void OnTxostenIdQueryChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        if (!ShellQueryLaguntzailea.SaiatuParseatuId(value, out var id))
            return;

        _txostenIdGordeta = id;
        _ = KargatuAsync();
    }

    [ObservableProperty]
    private bool _isKargatzean;

    [ObservableProperty]
    private string? _erroreMezua;

    [ObservableProperty]
    private string _helmuga = string.Empty;

    [ObservableProperty]
    private string _sailarenEtiketa = string.Empty;

    [ObservableProperty]
    private string _egoera = string.Empty;

    [ObservableProperty]
    private string _deskribapena = string.Empty;

    [ObservableProperty]
    private string _dataTestua = string.Empty;

    [ObservableProperty]
    private double _gastuenGuztira;

    [ObservableProperty]
    private string? _adminOharra;

    [ObservableProperty]
    private bool _adminOharraIkagarri;

    [ObservableProperty]
    private string? _argazkiUrl;

    [ObservableProperty]
    private bool _argazkiDago;

    [ObservableProperty]
    private string _garraioBideaTestua = string.Empty;

    [ObservableProperty]
    private bool _garraioBideaIkagarri;

    [ObservableProperty]
    private bool _ibilgailuaXehetasunakIkagarri;

    [ObservableProperty]
    private string _ibilgailuaMotaTestua = string.Empty;

    [ObservableProperty]
    private string _kilometroakBistaratzea = string.Empty;

    [ObservableProperty]
    private bool _kilometroakBistaratzeaIkagarri;

    public ObservableCollection<GastuLerroa> GastuLerroak { get; } = new();

    private async Task KargatuAsync()
    {
        ErroreMezua = null;
        GastuLerroak.Clear();
        GarraioBideaTestua = string.Empty;
        GarraioBideaIkagarri = false;
        IbilgailuaXehetasunakIkagarri = false;
        IbilgailuaMotaTestua = string.Empty;
        KilometroakBistaratzea = string.Empty;
        KilometroakBistaratzeaIkagarri = false;
        if (_txostenIdGordeta <= 0)
            return;

        try
        {
            IsKargatzean = true;

            var erabiltzaileId = await _autorizazioZerbitzua.EskuratuOraingoErabiltzaileIdAsync().ConfigureAwait(true);
            if (erabiltzaileId is null)
            {
                ErroreMezua = "Saioa iraungitu da. Berriz hasi saioa.";
                return;
            }

            var txostena = await _datuBaseaZerbitzua.EskuratuBidaiaTxostenaIdzAsync(_txostenIdGordeta).ConfigureAwait(true);
            if (txostena is null)
            {
                ErroreMezua = "Txartela ez da aurkitu.";
                return;
            }

            if (txostena.ErabiltzaileId != erabiltzaileId.Value)
            {
                ErroreMezua = "Ez duzu baimenik txartel hau ikusteko.";
                return;
            }

            Helmuga = txostena.Helmuga;
            SailarenEtiketa = SektoreaKargoarenHiztegia.LortuBaliozkotutakoSailaTestua(txostena.Saila);
            Egoera = txostena.Egoera;
            AdminOharra = txostena.AdminOharra;
            AdminOharraIkagarri = !string.IsNullOrWhiteSpace(txostena.AdminOharra);

            DataTestua = DataOrduaBalioak.DataOrduaBistaratu(txostena.HasieraData);

            var lerroak = await _datuBaseaZerbitzua.ZerrendatuGastuLerroakTxostenIdzAsync(_txostenIdGordeta).ConfigureAwait(true);
            double guztira = 0;
            double kilometroMax = 0;
            string? argazkia = null;
            var ibilgailuaBeharDu = false;
            var garraioPribatua = false;
            var garraioPublikoaHautatua = false;
            foreach (var lerroa in lerroak)
            {
                GastuLerroak.Add(lerroa);
                guztira += lerroa.ZenbatekoaGuztira;
                if (lerroa.Kilometroak > kilometroMax)
                    kilometroMax = lerroa.Kilometroak;
                if (lerroa.IbilgailuaBeharrezkoa == 1)
                    ibilgailuaBeharDu = true;
                if (GarraioBideaBalioak.IbilgailuaErabiltzenDu(lerroa.GarraioBidea))
                    garraioPribatua = true;
                if (string.Equals(lerroa.GarraioBidea.Trim(), GarraioBideaBalioak.GarraioPublikoa, StringComparison.Ordinal))
                    garraioPublikoaHautatua = true;
                if (string.IsNullOrWhiteSpace(argazkia) && !string.IsNullOrWhiteSpace(lerroa.TicketArgazkia))
                    argazkia = lerroa.TicketArgazkia;
                if (string.IsNullOrWhiteSpace(Deskribapena) && !string.IsNullOrWhiteSpace(lerroa.Oharrak))
                    Deskribapena = lerroa.Oharrak;
                if (!GarraioBideaIkagarri && !string.IsNullOrWhiteSpace(lerroa.GarraioBidea))
                {
                    GarraioBideaTestua = lerroa.GarraioBidea.Trim();
                    GarraioBideaIkagarri = true;
                }
            }

            IbilgailuaXehetasunakIkagarri = garraioPribatua || txostena.EmpresaIbilgailua == 1 || kilometroMax > 0
                || (ibilgailuaBeharDu && !garraioPublikoaHautatua);
            IbilgailuaMotaTestua = GarraioBideaBalioak.IbilgailuaMotaEtiketa(
                GarraioBideaIkagarri ? GarraioBideaTestua : lerroak.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l.GarraioBidea))?.GarraioBidea,
                txostena.EmpresaIbilgailua);
            if (string.IsNullOrWhiteSpace(IbilgailuaMotaTestua) && IbilgailuaXehetasunakIkagarri)
                IbilgailuaMotaTestua = txostena.EmpresaIbilgailua == 1
                    ? GarraioBideaBalioak.EnpresakoIbilgailua
                    : GarraioBideaBalioak.NorberarenIbilgailua;
            KilometroakBistaratzeaIkagarri = kilometroMax > 0;
            KilometroakBistaratzea = kilometroMax > 0
                ? kilometroMax.ToString("N0", CultureInfo.InvariantCulture)
                : string.Empty;

            GastuenGuztira = guztira;
            ArgazkiUrl = argazkia;
            ArgazkiDago = !string.IsNullOrWhiteSpace(argazkia);
        }
        catch (TursoExekuzioSalbuespena libEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                ?? "Datu-base errorea: ezin izan da irakurri. Saiatu berriro.";
            _logger.LogError(libEx, "LangileaTxartelaXehetasun: Turso errorea.");
        }
        catch (KeyNotFoundException knfEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ZutabeEskemaMezua;
            _logger.LogError(knfEx, "LangileaTxartelaXehetasun: mapa errorea.");
        }
        catch (SQLiteException sqlEx)
        {
            ErroreMezua = "Datu-base errorea: ezin izan da irakurri. Saiatu berriro.";
            _logger.LogError(sqlEx, "LangileaTxartelaXehetasun: SQLite errorea.");
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "LangileaTxartelaXehetasun: ustekabeko errorea.");
        }
        finally
        {
            IsKargatzean = false;
        }
    }

    [RelayCommand]
    private async Task IrekiArgazkiaHandianAsync()
    {
        if (string.IsNullOrWhiteSpace(ArgazkiUrl))
            return;

        try
        {
            await Launcher.Default.OpenAsync(new Uri(ArgazkiUrl)).ConfigureAwait(true);
        }
        catch (UriFormatException uriEx)
        {
            ErroreMezua = "Argazkiaren helbidea baliogabea da.";
            _logger.LogError(uriEx, "LangileaTxartelaXehetasun: argazki URL baliogabea.");
        }
        catch (FeatureNotSupportedException)
        {
            ErroreMezua = "Ezin da argazkia ireki gailu honetan.";
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ezin izan da argazkia ireki. Saiatu berriro.";
            _logger.LogError(ex, "LangileaTxartelaXehetasun: argazkia irekitzean errorea.");
        }
    }

    [RelayCommand]
    private async Task ItzeliAsync()
    {
        await Shell.Current.GoToAsync("..").ConfigureAwait(true);
    }
}
