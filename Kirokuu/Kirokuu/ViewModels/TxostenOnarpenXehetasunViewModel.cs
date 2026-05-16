using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.Zerbitzuak;
using Kirokuu.ZerbitzuakSaioa;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using SQLite;
using System.Globalization;
using System.Net.Http;

namespace Kirokuu.ViewModels;

[QueryProperty(nameof(TxostenIdQuery), "TxostenId")]
public partial class TxostenOnarpenXehetasunViewModel : ObservableObject
{
    private readonly AutorizazioZerbitzua _autorizazioZerbitzua;
    private readonly DatuBaseaZerbitzua _datuBaseaZerbitzua;
    private readonly SaioaGordetzeZerbitzua _saioaGordetzeZerbitzua;
    private readonly ILogger<TxostenOnarpenXehetasunViewModel> _logger;

    private int _txostenIdGordeta;

    private int? _adminSektoreIragazkia;

    public TxostenOnarpenXehetasunViewModel(
        AutorizazioZerbitzua autorizazioZerbitzua,
        DatuBaseaZerbitzua datuBaseaZerbitzua,
        SaioaGordetzeZerbitzua saioaGordetzeZerbitzua,
        ILogger<TxostenOnarpenXehetasunViewModel> logger)
    {
        _autorizazioZerbitzua = autorizazioZerbitzua ?? throw new ArgumentNullException(nameof(autorizazioZerbitzua));
        _datuBaseaZerbitzua = datuBaseaZerbitzua ?? throw new ArgumentNullException(nameof(datuBaseaZerbitzua));
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

            Helmuga = txostena.Helmuga;
            SailarenEtiketa = SektoreaKargoarenHiztegia.LortuBaliozkotutakoSailaTestua(txostena.Saila);
            Egoera = txostena.Egoera;
            OnarpenEkintzakIkagarri = string.Equals(txostena.Egoera, TxostenEgoera.Zain, StringComparison.Ordinal);

            var langile = await _datuBaseaZerbitzua.BilatuErabiltzaileLaburpenaIdzAsync(txostena.ErabiltzaileId).ConfigureAwait(true);
            LangileTestua = langile is null ? $"#{txostena.ErabiltzaileId}" : $"{langile.Izena} {langile.Abizena}";

            var lerroak = await _datuBaseaZerbitzua.ZerrendatuGastuLerroakTxostenIdzAsync(_txostenIdGordeta).ConfigureAwait(true);
            double guztira = 0;
            double kilometroMax = 0;
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
            }

            GastuenGuztira = guztira;
            AdminOharra = txostena.AdminOharra ?? string.Empty;
            JasoAurrerakina = txostena.JasoAurrekina;
            JasoAurrerakinaIkagarri = txostena.JasoAurrekina > 0;
            IbilgailuaEremuakIkagarri = garraioPribatua || txostena.EmpresaIbilgailua == 1 || kilometroMax > 0
                || (ibilgailuaBeharDu && !garraioPublikoaHautatua);
            IbilgailuaEremuakEditagarri = OnarpenEkintzakIkagarri && IbilgailuaEremuakIkagarri;
            EnpresakoIbilgailua = txostena.EmpresaIbilgailua == 1
                || lerroak.Any(l => GarraioBideaBalioak.DaEnpresakoIbilgailua(l.GarraioBidea));
            KilometroakTestua = kilometroMax > 0
                ? kilometroMax.ToString("0.##", CultureInfo.InvariantCulture)
                : string.Empty;
            OnPropertyChanged(nameof(OrdaintzekoBidea));
        }
        catch (TursoExekuzioSalbuespena libEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                ?? "Datu-base errorea: ezin izan da irakurri. Saiatu berriro.";
            _logger.LogError(libEx, "Txosten xehetasuna: Turso errorea.");
        }
        catch (KeyNotFoundException knfEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ZutabeEskemaMezua;
            _logger.LogError(knfEx, "Txosten xehetasuna: mapa errorea.");
        }
        catch (FormatException fmtEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.BalioFormatuMezua;
            _logger.LogError(fmtEx, "Txosten xehetasuna: formatu errorea.");
        }
        catch (SQLiteException sqlEx)
        {
            ErroreMezua = "Datu-base errorea: ezin izan da irakurri. Saiatu berriro.";
            _logger.LogError(sqlEx, "Txosten xehetasuna: SQLite errorea.");
        }
        catch (HttpRequestException httpEx)
        {
            ErroreMezua = "Sare errorea: konexioa egiaztatu eta saiatu berriro.";
            _logger.LogError(httpEx, "Txosten xehetasuna: sare errorea.");
        }
        catch (InvalidOperationException opEx)
        {
            ErroreMezua = "Eragiketa baliogabea. Berriz saiatu.";
            _logger.LogError(opEx, "Txosten xehetasuna: eragiketa baliogabea.");
        }
        catch (TaskCanceledException)
        {
            ErroreMezua = "Eskaerak denbora muga gainditu du. Saiatu berriro.";
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "Txosten xehetasuna: ustekabeko errorea.");
        }
        finally
        {
            IsKargatzean = false;
        }
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

        if (!double.TryParse(KilometroakTestua.Replace(',', '.'), NumberStyles.Any,
                CultureInfo.InvariantCulture, out var kilometroak) || kilometroak <= 0)
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

        double.TryParse(KilometroakTestua.Replace(',', '.'), NumberStyles.Any,
            CultureInfo.InvariantCulture, out var kilometroak);
        kilometroak = Math.Round(kilometroak, 2, MidpointRounding.AwayFromZero);

        var garraioTestua = GarraioBideaBalioak.SortuGarraioBideaTestua(EnpresakoIbilgailua);
        await _datuBaseaZerbitzua.EguneratuTxostenIbilgailuaEtaKilometroakAsync(
                _txostenIdGordeta,
                EnpresakoIbilgailua ? 1 : 0,
                garraioTestua,
                kilometroak)
            .ConfigureAwait(true);
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

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await BokadilloErakustzailea.SaiatuErakutsiAsync("Txostena onartu da.", _logger).ConfigureAwait(true);
            }).ConfigureAwait(true);

            await Shell.Current.GoToAsync("..").ConfigureAwait(true);
        }
        catch (TursoExekuzioSalbuespena libEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                ?? "Datu-base errorea: ezin izan da gorde. Saiatu berriro.";
            _logger.LogError(libEx, "Txosten onartu: Turso errorea.");
        }
        catch (SQLiteException sqlEx)
        {
            ErroreMezua = "Datu-base errorea: ezin izan da gorde. Saiatu berriro.";
            _logger.LogError(sqlEx, "Txosten onartu: SQLite errorea.");
        }
        catch (InvalidOperationException opEx)
        {
            ErroreMezua = "Eragiketa baliogabea. Berriz saiatu.";
            _logger.LogError(opEx, "Txosten onartu: eragiketa baliogabea.");
        }
        catch (HttpRequestException httpEx)
        {
            ErroreMezua = "Sare errorea: konexioa egiaztatu eta saiatu berriro.";
            _logger.LogError(httpEx, "Txosten onartu: sare errorea.");
        }
        catch (TaskCanceledException)
        {
            ErroreMezua = "Eskaerak denbora muga gainditu du. Saiatu berriro.";
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "Txosten onartu: ustekabeko errorea.");
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

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await BokadilloErakustzailea.SaiatuErakutsiAsync("Txostena ukatu da.", _logger).ConfigureAwait(true);
            }).ConfigureAwait(true);

            await Shell.Current.GoToAsync("..").ConfigureAwait(true);
        }
        catch (TursoExekuzioSalbuespena libEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                ?? "Datu-base errorea: ezin izan da gorde. Saiatu berriro.";
            _logger.LogError(libEx, "Txosten ukatu: Turso errorea.");
        }
        catch (SQLiteException sqlEx)
        {
            ErroreMezua = "Datu-base errorea: ezin izan da gorde. Saiatu berriro.";
            _logger.LogError(sqlEx, "Txosten ukatu: SQLite errorea.");
        }
        catch (InvalidOperationException opEx)
        {
            ErroreMezua = "Eragiketa baliogabea. Berriz saiatu.";
            _logger.LogError(opEx, "Txosten ukatu: eragiketa baliogabea.");
        }
        catch (HttpRequestException httpEx)
        {
            ErroreMezua = "Sare errorea: konexioa egiaztatu eta saiatu berriro.";
            _logger.LogError(httpEx, "Txosten ukatu: sare errorea.");
        }
        catch (TaskCanceledException)
        {
            ErroreMezua = "Eskaerak denbora muga gainditu du. Saiatu berriro.";
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "Txosten ukatu: ustekabeko errorea.");
        }
        finally
        {
            IsKargatzean = false;
        }
    }
}
