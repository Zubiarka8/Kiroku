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

[QueryProperty(nameof(SarreraIdQuery), "SarreraId")]
public partial class DiruSarreraOnarpenXehetasunViewModel : ObservableObject
{
    private readonly AutorizazioZerbitzua _autorizazioZerbitzua;
    private readonly DatuBaseaZerbitzua _datuBaseaZerbitzua;
    private readonly SaioaGordetzeZerbitzua _saioaGordetzeZerbitzua;
    private readonly ILogger<DiruSarreraOnarpenXehetasunViewModel> _logger;

    private string _sarreraIdGordeta = string.Empty;

    public DiruSarreraOnarpenXehetasunViewModel(
        AutorizazioZerbitzua autorizazioZerbitzua,
        DatuBaseaZerbitzua datuBaseaZerbitzua,
        SaioaGordetzeZerbitzua saioaGordetzeZerbitzua,
        ILogger<DiruSarreraOnarpenXehetasunViewModel> logger)
    {
        _autorizazioZerbitzua = autorizazioZerbitzua ?? throw new ArgumentNullException(nameof(autorizazioZerbitzua));
        _datuBaseaZerbitzua = datuBaseaZerbitzua ?? throw new ArgumentNullException(nameof(datuBaseaZerbitzua));
        _saioaGordetzeZerbitzua = saioaGordetzeZerbitzua ?? throw new ArgumentNullException(nameof(saioaGordetzeZerbitzua));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [ObservableProperty]
    private string _sarreraIdQuery = string.Empty;

    partial void OnSarreraIdQueryChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        _sarreraIdGordeta = Uri.UnescapeDataString(value.Trim());
        _ = KargatuAsync();
    }

    [ObservableProperty]
    private bool _isKargatzean;

    [ObservableProperty]
    private string? _erroreMezua;

    [ObservableProperty]
    private string _deskribapena = string.Empty;

    [ObservableProperty]
    private string _egoera = string.Empty;

    [ObservableProperty]
    private double _zenbatekoa;

    [ObservableProperty]
    private string _langileTestua = string.Empty;

    [ObservableProperty]
    private string _dataTestua = string.Empty;

    [ObservableProperty]
    private bool _onarpenEkintzakIkagarri;

    [ObservableProperty]
    private string _adminOharra = string.Empty;

    private async Task KargatuAsync()
    {
        ErroreMezua = null;
        if (string.IsNullOrWhiteSpace(_sarreraIdGordeta))
            return;

        try
        {
            IsKargatzean = true;
            if (!await _autorizazioZerbitzua.DaAdministratzaileaAsync().ConfigureAwait(true))
            {
                ErroreMezua = "Ez duzu baimenik.";
                return;
            }

            var sarrera = await _datuBaseaZerbitzua.EskuratuDiruSarreraIdzAsync(_sarreraIdGordeta).ConfigureAwait(true);
            if (sarrera is null)
            {
                ErroreMezua = "Diru-sarrera ez da aurkitu.";
                return;
            }

            Deskribapena = sarrera.Deskribapena;
            Egoera = sarrera.Egoera;
            Zenbatekoa = sarrera.Zenbatekoa;
            DataTestua = sarrera.DataTestua;
            AdminOharra = sarrera.AdminOharra ?? string.Empty;
            OnarpenEkintzakIkagarri = string.Equals(sarrera.Egoera, TxostenEgoera.Zain, StringComparison.Ordinal);

            var langile = await _datuBaseaZerbitzua.BilatuErabiltzaileLaburpenaIdzAsync(sarrera.ErabiltzaileId).ConfigureAwait(true);
            LangileTestua = langile is null ? $"#{sarrera.ErabiltzaileId}" : $"{langile.Izena} {langile.Abizena}";
        }
        catch (TursoExekuzioSalbuespena libEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                ?? "Datu-base errorea: ezin izan da irakurri. Saiatu berriro.";
            _logger.LogError(libEx, "Diru-sarrera xehetasuna: Turso errorea.");
        }
        catch (KeyNotFoundException knfEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ZutabeEskemaMezua;
            _logger.LogError(knfEx, "Diru-sarrera xehetasuna: mapa errorea.");
        }
        catch (FormatException fmtEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.BalioFormatuMezua;
            _logger.LogError(fmtEx, "Diru-sarrera xehetasuna: formatu errorea.");
        }
        catch (SQLiteException sqlEx)
        {
            ErroreMezua = "Datu-base errorea: ezin izan da irakurri. Saiatu berriro.";
            _logger.LogError(sqlEx, "Diru-sarrera xehetasuna: SQLite errorea.");
        }
        catch (HttpRequestException httpEx)
        {
            ErroreMezua = "Sare errorea: konexioa egiaztatu eta saiatu berriro.";
            _logger.LogError(httpEx, "Diru-sarrera xehetasuna: sare errorea.");
        }
        catch (InvalidOperationException opEx)
        {
            ErroreMezua = "Eragiketa baliogabea. Berriz saiatu.";
            _logger.LogError(opEx, "Diru-sarrera xehetasuna: eragiketa baliogabea.");
        }
        catch (TaskCanceledException)
        {
            ErroreMezua = "Eskaerak denbora muga gainditu du. Saiatu berriro.";
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "Diru-sarrera xehetasuna: ustekabeko errorea.");
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
        if (string.IsNullOrWhiteSpace(_sarreraIdGordeta))
            return;

        try
        {
            IsKargatzean = true;
            var adminId = await EskuratuAdministratzaileIdAsync().ConfigureAwait(true);
            await _datuBaseaZerbitzua.EguneratuDiruSarreraEgoeraAdministratzaileAsync(
                    _sarreraIdGordeta,
                    TxostenEgoera.Onartua,
                    null,
                    adminId)
                .ConfigureAwait(true);

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await BokadilloErakustzailea.SaiatuErakutsiAsync("Diru-sarrera onartu da.", _logger).ConfigureAwait(true);
            }).ConfigureAwait(true);

            await Shell.Current.GoToAsync("..").ConfigureAwait(true);
        }
        catch (TursoExekuzioSalbuespena libEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                ?? "Datu-base errorea: ezin izan da gorde. Saiatu berriro.";
            _logger.LogError(libEx, "Diru-sarrera onartu: Turso errorea.");
        }
        catch (SQLiteException sqlEx)
        {
            ErroreMezua = "Datu-base errorea: ezin izan da gorde. Saiatu berriro.";
            _logger.LogError(sqlEx, "Diru-sarrera onartu: SQLite errorea.");
        }
        catch (InvalidOperationException opEx)
        {
            ErroreMezua = "Eragiketa baliogabea. Berriz saiatu.";
            _logger.LogError(opEx, "Diru-sarrera onartu: eragiketa baliogabea.");
        }
        catch (HttpRequestException httpEx)
        {
            ErroreMezua = "Sare errorea: konexioa egiaztatu eta saiatu berriro.";
            _logger.LogError(httpEx, "Diru-sarrera onartu: sare errorea.");
        }
        catch (TaskCanceledException)
        {
            ErroreMezua = "Eskaerak denbora muga gainditu du. Saiatu berriro.";
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "Diru-sarrera onartu: ustekabeko errorea.");
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
        if (string.IsNullOrWhiteSpace(_sarreraIdGordeta))
            return;

        try
        {
            IsKargatzean = true;
            var adminId = await EskuratuAdministratzaileIdAsync().ConfigureAwait(true);
            await _datuBaseaZerbitzua.EguneratuDiruSarreraEgoeraAdministratzaileAsync(
                    _sarreraIdGordeta,
                    TxostenEgoera.Ukatua,
                    string.IsNullOrWhiteSpace(AdminOharra) ? null : AdminOharra.Trim(),
                    adminId)
                .ConfigureAwait(true);

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await BokadilloErakustzailea.SaiatuErakutsiAsync("Diru-sarrera ukatu da.", _logger).ConfigureAwait(true);
            }).ConfigureAwait(true);

            await Shell.Current.GoToAsync("..").ConfigureAwait(true);
        }
        catch (TursoExekuzioSalbuespena libEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                ?? "Datu-base errorea: ezin izan da gorde. Saiatu berriro.";
            _logger.LogError(libEx, "Diru-sarrera ukatu: Turso errorea.");
        }
        catch (SQLiteException sqlEx)
        {
            ErroreMezua = "Datu-base errorea: ezin izan da gorde. Saiatu berriro.";
            _logger.LogError(sqlEx, "Diru-sarrera ukatu: SQLite errorea.");
        }
        catch (InvalidOperationException opEx)
        {
            ErroreMezua = "Eragiketa baliogabea. Berriz saiatu.";
            _logger.LogError(opEx, "Diru-sarrera ukatu: eragiketa baliogabea.");
        }
        catch (HttpRequestException httpEx)
        {
            ErroreMezua = "Sare errorea: konexioa egiaztatu eta saiatu berriro.";
            _logger.LogError(httpEx, "Diru-sarrera ukatu: sare errorea.");
        }
        catch (TaskCanceledException)
        {
            ErroreMezua = "Eskaerak denbora muga gainditu du. Saiatu berriro.";
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "Diru-sarrera ukatu: ustekabeko errorea.");
        }
        finally
        {
            IsKargatzean = false;
        }
    }
}
