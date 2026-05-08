using System.Collections.ObjectModel;
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

    private string _txostenIdGordeta = string.Empty;

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

        _txostenIdGordeta = Uri.UnescapeDataString(value.Trim());
        _ = KargatuAsync();
    }

    [ObservableProperty]
    private bool _isKargatzean;

    [ObservableProperty]
    private string? _erroreMezua;

    [ObservableProperty]
    private string _helmuga = string.Empty;

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

    public ObservableCollection<GastuLerroa> GastuLerroak { get; } = new();

    private async Task KargatuAsync()
    {
        ErroreMezua = null;
        GastuLerroak.Clear();
        if (string.IsNullOrWhiteSpace(_txostenIdGordeta))
            return;

        try
        {
            IsKargatzean = true;
            if (!await _autorizazioZerbitzua.DaAdministratzaileaAsync().ConfigureAwait(true))
            {
                ErroreMezua = "Ez duzu baimenik.";
                return;
            }

            var txostena = await _datuBaseaZerbitzua.EskuratuBidaiaTxostenaIdzAsync(_txostenIdGordeta).ConfigureAwait(true);
            if (txostena is null)
            {
                ErroreMezua = "Txostena ez da aurkitu.";
                return;
            }

            Helmuga = txostena.Helmuga;
            Egoera = txostena.Egoera;
            OnarpenEkintzakIkagarri = string.Equals(txostena.Egoera, TxostenEgoera.Zain, StringComparison.Ordinal);

            var langile = await _datuBaseaZerbitzua.BilatuErabiltzaileLaburpenaIdzAsync(txostena.ErabiltzaileId).ConfigureAwait(true);
            LangileTestua = langile is null ? $"#{txostena.ErabiltzaileId}" : $"{langile.Izena} {langile.Abizena}";

            var lerroak = await _datuBaseaZerbitzua.ZerrendatuGastuLerroakTxostenIdzAsync(_txostenIdGordeta).ConfigureAwait(true);
            double guztira = 0;
            foreach (var lerroa in lerroak)
            {
                GastuLerroak.Add(lerroa);
                guztira += lerroa.ZenbatekoaGuztira;
            }

            GastuenGuztira = guztira;
            AdminOharra = txostena.AdminOharra ?? string.Empty;
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

    [RelayCommand]
    private async Task OnartuAsync()
    {
        ErroreMezua = null;
        if (string.IsNullOrWhiteSpace(_txostenIdGordeta))
            return;

        try
        {
            IsKargatzean = true;
            var adminId = await EskuratuAdministratzaileIdAsync().ConfigureAwait(true);
            await _datuBaseaZerbitzua.EguneratuTxostenEgoeraAdministratzaileAsync(
                    _txostenIdGordeta,
                    TxostenEgoera.Onartua,
                    null,
                    adminId)
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
        if (string.IsNullOrWhiteSpace(_txostenIdGordeta))
            return;

        try
        {
            IsKargatzean = true;
            var adminId = await EskuratuAdministratzaileIdAsync().ConfigureAwait(true);
            await _datuBaseaZerbitzua.EguneratuTxostenEgoeraAdministratzaileAsync(
                    _txostenIdGordeta,
                    TxostenEgoera.Ukatua,
                    string.IsNullOrWhiteSpace(AdminOharra) ? null : AdminOharra.Trim(),
                    adminId)
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
