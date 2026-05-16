using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.DatuEreduak;
using Kirokuu.Laguntzaileak;
using Kirokuu.Zerbitzuak;
using Kirokuu.ZerbitzuakSaioa;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace Kirokuu.ViewModels;

[QueryProperty(nameof(TxostenIdQuery), "TxostenId")]
public partial class TxostenOnarpenXehetasunViewModel : ObservableObject
{
    private readonly AutorizazioZerbitzua _autorizazioZerbitzua;
    private readonly DatuBaseaZerbitzua _datuBaseaZerbitzua;
    private readonly AuditoretzaZerbitzua _auditoretzaZerbitzua;
    private readonly SaioaGordetzeZerbitzua _saioaGordetzeZerbitzua;
    private readonly ILogger<TxostenOnarpenXehetasunViewModel> _logger;

    private int _txostenIdGordeta;

    private int _txostenaErabiltzaileId;

    private int? _adminSektoreIragazkia;

    public TxostenOnarpenXehetasunViewModel(
        AutorizazioZerbitzua autorizazioZerbitzua,
        DatuBaseaZerbitzua datuBaseaZerbitzua,
        AuditoretzaZerbitzua auditoretzaZerbitzua,
        SaioaGordetzeZerbitzua saioaGordetzeZerbitzua,
        ILogger<TxostenOnarpenXehetasunViewModel> logger)
    {
        _autorizazioZerbitzua = autorizazioZerbitzua ?? throw new ArgumentNullException(nameof(autorizazioZerbitzua));
        _datuBaseaZerbitzua = datuBaseaZerbitzua ?? throw new ArgumentNullException(nameof(datuBaseaZerbitzua));
        _auditoretzaZerbitzua = auditoretzaZerbitzua ?? throw new ArgumentNullException(nameof(auditoretzaZerbitzua));
        _saioaGordetzeZerbitzua = saioaGordetzeZerbitzua ?? throw new ArgumentNullException(nameof(saioaGordetzeZerbitzua));
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
    private string _langileTestua = string.Empty;

    [ObservableProperty]
    private double _gastuenGuztira;

    [ObservableProperty]
    private bool _onarpenEkintzakIkagarri;

    [ObservableProperty]
    private string _adminOharra = string.Empty;

    [ObservableProperty]
    private int _jasoAurrerakina;

    [ObservableProperty]
    private bool _jasoAurrerakinaIkagarri;

    [ObservableProperty]
    private bool _ibilgailuaEremuakIkagarri;

    [ObservableProperty]
    private bool _ibilgailuaEremuakEditagarri;

    [ObservableProperty]
    private bool _enpresakoIbilgailua;

    [ObservableProperty]
    private string _kilometroakTestua = string.Empty;

    public double OrdaintzekoBidea => GastuenGuztira - JasoAurrerakina;

    public ObservableCollection<GastuLerroa> GastuLerroak { get; } = new();

    private async Task KargatuAsync()
    {
        ErroreMezua = null;
        GastuLerroak.Clear();
        if (_txostenIdGordeta <= 0)
            return;

        try
        {
            IsKargatzean = true;
            if (!await _autorizazioZerbitzua.DaAdministratzaileaAsync().ConfigureAwait(true))
            {
                ErroreMezua = "Ez duzu baimenik.";
                return;
            }

            _adminSektoreIragazkia = await _autorizazioZerbitzua
                .EskuratuAdminSektoreIragazkiaAsync()
                .ConfigureAwait(true);

            var txostena = await _datuBaseaZerbitzua
                .EskuratuBidaiaTxostenaAdministratzailearentzatAsync(_txostenIdGordeta, _adminSektoreIragazkia)
                .ConfigureAwait(true);
            if (txostena is null)
            {
                ErroreMezua = "Txostena ez da aurkitu edo ez duzu baimenik.";
                return;
            }

            var langile = await _datuBaseaZerbitzua.BilatuErabiltzaileLaburpenaIdzAsync(txostena.ErabiltzaileId).ConfigureAwait(true);
            var langileTestua = langile is null ? $"#{txostena.ErabiltzaileId}" : $"{langile.Izena} {langile.Abizena}";

            var lerroak = await _datuBaseaZerbitzua.ZerrendatuGastuLerroakTxostenIdzAsync(_txostenIdGordeta).ConfigureAwait(true);
            var ikuspegia = TxartelXehetasunLaguntzailea.EraikiIkuspegia(
                txostena,
                lerroak,
                langileTestua,
                administratzaileIkuspegia: true);
            AplikatuIkuspegia(ikuspegia);
        }
        catch (Exception ex)
        {
            ViewModelSalbuespenTratatzailea.TratatuIrakurketa(ex, m => ErroreMezua = m, _logger, "Txosten xehetasuna");
        }
        finally
        {
            IsKargatzean = false;
        }
    }

    private void AplikatuIkuspegia(TxartelXehetasunIkuspegia ikuspegia)
    {
        _txostenaErabiltzaileId = ikuspegia.TxostenaErabiltzaileId;
        Helmuga = ikuspegia.Helmuga;
        SailarenEtiketa = ikuspegia.SailarenEtiketa;
        Egoera = ikuspegia.Egoera;
        LangileTestua = ikuspegia.LangileTestua;
        GastuenGuztira = ikuspegia.GastuenGuztira;
        OnarpenEkintzakIkagarri = ikuspegia.OnarpenEkintzakIkagarri;
        AdminOharra = ikuspegia.AdminOharra ?? string.Empty;
        JasoAurrerakina = ikuspegia.JasoAurrerakina;
        JasoAurrerakinaIkagarri = ikuspegia.JasoAurrerakinaIkagarri;
        IbilgailuaEremuakIkagarri = ikuspegia.IbilgailuaEremuakIkagarri;
        IbilgailuaEremuakEditagarri = ikuspegia.IbilgailuaEremuakEditagarri;
        EnpresakoIbilgailua = ikuspegia.EnpresakoIbilgailua;
        KilometroakTestua = ikuspegia.KilometroakTestua;

        GastuLerroak.Clear();
        foreach (var lerroa in ikuspegia.GastuLerroak)
            GastuLerroak.Add(lerroa);

        OnPropertyChanged(nameof(OrdaintzekoBidea));
    }

    private async Task<int> EskuratuAdministratzaileIdAsync()
    {
        var testua = await _saioaGordetzeZerbitzua.IrakurriErabiltzaileIdTestuaAsync().ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(testua) ||
            !int.TryParse(testua, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            throw new InvalidOperationException("Administratzailearen IDa ez da eskuragarri.");

        return id;
    }

    private bool SaiatuBalidatuIbilgailuaDatuak()
    {
        if (!IbilgailuaEremuakIkagarri || !IbilgailuaEremuakEditagarri)
            return true;

        if (!ZenbatekoaBalidazioLaguntzailea.SaiatuParseatuDezimala(KilometroakTestua, out var kilometroak) ||
            kilometroak <= 0)
        {
            ErroreMezua = "Sartu kilometro kopurua (0 baino handiagoa).";
            return false;
        }

        return true;
    }

    private async Task GordetuIbilgailuaDatuakAsync()
    {
        if (!IbilgailuaEremuakIkagarri || !IbilgailuaEremuakEditagarri)
            return;

        ZenbatekoaBalidazioLaguntzailea.SaiatuParseatuDezimala(KilometroakTestua, out var kilometroak);
        kilometroak = Math.Round(kilometroak, 2, MidpointRounding.AwayFromZero);

        var garraioTestua = GarraioBideaBalioak.SortuGarraioBideaTestua(EnpresakoIbilgailua);
        await _datuBaseaZerbitzua.EguneratuTxostenIbilgailuaEtaKilometroakAsync(
                _txostenIdGordeta,
                EnpresakoIbilgailua ? 1 : 0,
                garraioTestua,
                kilometroak)
            .ConfigureAwait(true);
    }

    private async Task IdatziOnarpenAuditoreaAsync(string egoeraBerria, int adminId)
    {
        var ekintza = string.Equals(egoeraBerria, TxostenEgoera.Onartua, StringComparison.Ordinal)
            ? AuditoretzaEkintzak.TxostenaOnartua
            : AuditoretzaEkintzak.TxostenaEzeztatu;
        var deskribapena = $"{_txostenIdGordeta} · {egoeraBerria}";
        await _auditoretzaZerbitzua.IdazkiLogaAsync(
            ekintza,
            deskribapena,
            adminId,
            _txostenIdGordeta,
            _txostenaErabiltzaileId).ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task OnartuAsync()
    {
        ErroreMezua = null;
        if (_txostenIdGordeta <= 0)
            return;

        if (!SaiatuBalidatuIbilgailuaDatuak())
            return;

        try
        {
            IsKargatzean = true;
            await GordetuIbilgailuaDatuakAsync().ConfigureAwait(true);
            var adminId = await EskuratuAdministratzaileIdAsync().ConfigureAwait(true);
            await _datuBaseaZerbitzua.EguneratuTxostenEgoeraAdministratzaileAsync(
                    _txostenIdGordeta,
                    TxostenEgoera.Onartua,
                    null,
                    adminId,
                    _adminSektoreIragazkia)
                .ConfigureAwait(true);
            await IdatziOnarpenAuditoreaAsync(TxostenEgoera.Onartua, adminId).ConfigureAwait(true);

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await BokadilloErakustzailea.SaiatuErakutsiAsync("Txostena onartu da.", _logger).ConfigureAwait(true);
            }).ConfigureAwait(true);

            await Shell.Current.GoToAsync("..").ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            ViewModelSalbuespenTratatzailea.TratatuIdazketa(ex, m => ErroreMezua = m, _logger, "Txosten onartu");
        }
        finally
        {
            IsKargatzean = false;
        }
    }

    [RelayCommand]
    private async Task UkatuAsync()
    {
        ErroreMezua = null;
        if (_txostenIdGordeta <= 0)
            return;

        if (!SaiatuBalidatuIbilgailuaDatuak())
            return;

        try
        {
            IsKargatzean = true;
            await GordetuIbilgailuaDatuakAsync().ConfigureAwait(true);
            var adminId = await EskuratuAdministratzaileIdAsync().ConfigureAwait(true);
            await _datuBaseaZerbitzua.EguneratuTxostenEgoeraAdministratzaileAsync(
                    _txostenIdGordeta,
                    TxostenEgoera.Ukatua,
                    string.IsNullOrWhiteSpace(AdminOharra) ? null : AdminOharra.Trim(),
                    adminId,
                    _adminSektoreIragazkia)
                .ConfigureAwait(true);
            await IdatziOnarpenAuditoreaAsync(TxostenEgoera.Ukatua, adminId).ConfigureAwait(true);

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await BokadilloErakustzailea.SaiatuErakutsiAsync("Txostena ukatu da.", _logger).ConfigureAwait(true);
            }).ConfigureAwait(true);

            await Shell.Current.GoToAsync("..").ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            ViewModelSalbuespenTratatzailea.TratatuIdazketa(ex, m => ErroreMezua = m, _logger, "Txosten ukatu");
        }
        finally
        {
            IsKargatzean = false;
        }
    }
}
