using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.DatuEreduak;
using Kirokuu.Laguntzaileak;
using Kirokuu.Zerbitzuak;
using Kirokuu.ZerbitzuakSaioa;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;

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

            var lerroak = await _datuBaseaZerbitzua.ZerrendatuGastuLerroakTxostenIdzAsync(_txostenIdGordeta).ConfigureAwait(true);
            var ikuspegia = TxartelXehetasunLaguntzailea.EraikiIkuspegia(txostena, lerroak);
            AplikatuIkuspegia(ikuspegia);
        }
        catch (Exception ex)
        {
            ViewModelSalbuespenTratatzailea.TratatuIrakurketa(ex, m => ErroreMezua = m, _logger, "LangileaTxartelaXehetasun");
        }
        finally
        {
            IsKargatzean = false;
        }
    }

    private void AplikatuIkuspegia(TxartelXehetasunIkuspegia ikuspegia)
    {
        Helmuga = ikuspegia.Helmuga;
        SailarenEtiketa = ikuspegia.SailarenEtiketa;
        Egoera = ikuspegia.Egoera;
        Deskribapena = ikuspegia.Deskribapena;
        DataTestua = ikuspegia.DataTestua;
        GastuenGuztira = ikuspegia.GastuenGuztira;
        AdminOharra = ikuspegia.AdminOharra;
        AdminOharraIkagarri = ikuspegia.AdminOharraIkagarri;
        ArgazkiUrl = ikuspegia.ArgazkiUrl;
        ArgazkiDago = ikuspegia.ArgazkiDago;
        GarraioBideaTestua = ikuspegia.GarraioBideaTestua;
        GarraioBideaIkagarri = ikuspegia.GarraioBideaIkagarri;
        IbilgailuaXehetasunakIkagarri = ikuspegia.IbilgailuaXehetasunakIkagarri;
        IbilgailuaMotaTestua = ikuspegia.IbilgailuaMotaTestua;
        KilometroakBistaratzea = ikuspegia.KilometroakBistaratzea;
        KilometroakBistaratzeaIkagarri = ikuspegia.KilometroakBistaratzeaIkagarri;

        GastuLerroak.Clear();
        foreach (var lerroa in ikuspegia.GastuLerroak)
            GastuLerroak.Add(lerroa);
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
        catch (Exception ex)
        {
            if (!ViewModelSalbuespenTratatzailea.TratatuIrakurketa(ex, m => ErroreMezua = m, _logger, "LangileaTxartelaXehetasun argazkia"))
                ErroreMezua = "Ezin izan da argazkia ireki. Saiatu berriro.";
        }
    }

    [RelayCommand]
    private async Task ItzeliAsync()
    {
        await Shell.Current.GoToAsync("..").ConfigureAwait(true);
    }
}
