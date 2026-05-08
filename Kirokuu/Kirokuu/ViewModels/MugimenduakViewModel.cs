using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.DatuEreduak;
using Kirokuu.Pages;
using Kirokuu.Zerbitzuak;
using Kirokuu.ZerbitzuakSaioa;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using SQLite;
using System.Net.Http;

namespace Kirokuu.ViewModels;

public partial class MugimenduakViewModel : ObservableObject
{
    private readonly AutorizazioZerbitzua _autorizazioZerbitzua;
    private readonly DatuBaseaZerbitzua _datuBaseaZerbitzua;
    private readonly ILogger<MugimenduakViewModel> _logger;

    public MugimenduakViewModel(
        AutorizazioZerbitzua autorizazioZerbitzua,
        DatuBaseaZerbitzua datuBaseaZerbitzua,
        ILogger<MugimenduakViewModel> logger)
    {
        _autorizazioZerbitzua = autorizazioZerbitzua ?? throw new ArgumentNullException(nameof(autorizazioZerbitzua));
        _datuBaseaZerbitzua = datuBaseaZerbitzua ?? throw new ArgumentNullException(nameof(datuBaseaZerbitzua));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [ObservableProperty]
    private bool _isKargatzean;

    [ObservableProperty]
    private string? _erroreMezua;

    [ObservableProperty]
    private bool _gastuAtalaNagusia = true;

    public ObservableCollection<TxostenOnarpenLaburpena> TxostenZainak { get; } = new();

    public ObservableCollection<DiruSarreraOnarpenLaburpena> DiruSarreraZainak { get; } = new();

    [RelayCommand]
    private async Task AgertzenDeneanAsync()
    {
        ErroreMezua = null;
        TxostenZainak.Clear();
        DiruSarreraZainak.Clear();

        try
        {
            IsKargatzean = true;
            if (!await _autorizazioZerbitzua.DaAdministratzaileaAsync().ConfigureAwait(true))
            {
                ErroreMezua = "Ez duzu baimenik atal honetan.";
                return;
            }

            var txostenak = await _datuBaseaZerbitzua.ZerrendatuTxostenOnarpenLaburrakAsync(TxostenEgoera.Zain).ConfigureAwait(true);
            foreach (var t in txostenak)
                TxostenZainak.Add(t);

            var sarrerak = await _datuBaseaZerbitzua.ZerrendatuDiruSarreraOnarpenLaburrakAsync(TxostenEgoera.Zain).ConfigureAwait(true);
            foreach (var s in sarrerak)
                DiruSarreraZainak.Add(s);
        }
        catch (TursoExekuzioSalbuespena libEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                ?? "Datu-base errorea: ezin izan da irakurri. Saiatu berriro.";
            _logger.LogError(libEx, "Mugimenduak: Turso errorea.");
        }
        catch (KeyNotFoundException knfEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ZutabeEskemaMezua;
            _logger.LogError(knfEx, "Mugimenduak: mapa errorea.");
        }
        catch (FormatException fmtEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.BalioFormatuMezua;
            _logger.LogError(fmtEx, "Mugimenduak: formatu errorea.");
        }
        catch (SQLiteException sqlEx)
        {
            ErroreMezua = "Datu-base errorea: ezin izan da irakurri. Saiatu berriro.";
            _logger.LogError(sqlEx, "Mugimenduak: SQLite errorea.");
        }
        catch (HttpRequestException httpEx)
        {
            ErroreMezua = "Sare errorea: konexioa egiaztatu eta saiatu berriro.";
            _logger.LogError(httpEx, "Mugimenduak: sare errorea.");
        }
        catch (InvalidOperationException opEx)
        {
            ErroreMezua = "Eragiketa baliogabea. Berriz saiatu saioa hasita.";
            _logger.LogError(opEx, "Mugimenduak: eragiketa baliogabea.");
        }
        catch (TaskCanceledException)
        {
            ErroreMezua = "Eskaerak denbora muga gainditu du. Saiatu berriro.";
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "Mugimenduak: ustekabeko errorea.");
        }
        finally
        {
            IsKargatzean = false;
        }
    }

    [RelayCommand]
    private void HautatuGastuAtala() => GastuAtalaNagusia = true;

    [RelayCommand]
    private void HautatuDiruSarreraAtala() => GastuAtalaNagusia = false;

    [RelayCommand]
    private async Task IrekiTxostenXehetasunaAsync(TxostenOnarpenLaburpena? laburpena)
    {
        if (laburpena is null || string.IsNullOrWhiteSpace(laburpena.TxostenId))
            return;

        await Shell.Current
            .GoToAsync($"{nameof(TxostenOnarpenXehetasunOrria)}?TxostenId={Uri.EscapeDataString(laburpena.TxostenId)}")
            .ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task IrekiDiruSarreraXehetasunaAsync(DiruSarreraOnarpenLaburpena? laburpena)
    {
        if (laburpena is null || string.IsNullOrWhiteSpace(laburpena.SarreraId))
            return;

        await Shell.Current
            .GoToAsync($"{nameof(DiruSarreraOnarpenXehetasunOrria)}?SarreraId={Uri.EscapeDataString(laburpena.SarreraId)}")
            .ConfigureAwait(true);
    }
}
