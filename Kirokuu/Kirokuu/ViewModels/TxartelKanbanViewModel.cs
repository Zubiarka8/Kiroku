using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.DatuEreduak;
using Kirokuu.Pages;
using Kirokuu.Zerbitzuak;
using Kirokuu.ZerbitzuakSaioa;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using SQLite;

namespace Kirokuu.ViewModels;

public partial class TxartelKanbanViewModel : ObservableObject
{
    private readonly DatuBaseaZerbitzua _datuBaseaZerbitzua;
    private readonly AutorizazioZerbitzua _autorizazioZerbitzua;
    private readonly BerrespenLeihoZerbitzua _berrespenLeiho;
    private readonly ILogger<TxartelKanbanViewModel> _logger;

    public TxartelKanbanViewModel(
        DatuBaseaZerbitzua datuBaseaZerbitzua,
        AutorizazioZerbitzua autorizazioZerbitzua,
        BerrespenLeihoZerbitzua berrespenLeiho,
        ILogger<TxartelKanbanViewModel> logger)
    {
        _datuBaseaZerbitzua = datuBaseaZerbitzua ?? throw new ArgumentNullException(nameof(datuBaseaZerbitzua));
        _autorizazioZerbitzua = autorizazioZerbitzua ?? throw new ArgumentNullException(nameof(autorizazioZerbitzua));
        _berrespenLeiho = berrespenLeiho ?? throw new ArgumentNullException(nameof(berrespenLeiho));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [ObservableProperty]
    private bool _isKargatzean;

    [ObservableProperty]
    private string? _erroreMezua;

    public ObservableCollection<TxostenOnarpenLaburpena> ZainZerrenda { get; } = new();
    public ObservableCollection<TxostenOnarpenLaburpena> OnartuaZerrenda { get; } = new();
    public ObservableCollection<TxostenOnarpenLaburpena> UkatuaZerrenda { get; } = new();
    public ObservableCollection<TxostenOnarpenLaburpena> EzeztatuaZerrenda { get; } = new();

    [RelayCommand]
    private async Task AgertzenDeneanAsync()
    {
        if (IsKargatzean) return;
        ErroreMezua = null;
        ZainZerrenda.Clear();
        OnartuaZerrenda.Clear();
        UkatuaZerrenda.Clear();
        EzeztatuaZerrenda.Clear();

        try
        {
            IsKargatzean = true;

            var erabiltzaileId = await _autorizazioZerbitzua.EskuratuOraingoErabiltzaileIdAsync().ConfigureAwait(true);
            if (erabiltzaileId is null)
            {
                ErroreMezua = "Saioa iraungitu da. Berriz hasi saioa.";
                return;
            }

            var zerrenda = await _datuBaseaZerbitzua
                .ZerrendatuLangilerenTxostenakAsync(erabiltzaileId.Value)
                .ConfigureAwait(true);

            foreach (var t in zerrenda)
            {
                if (t.Egoera == DatuBasea.Ereduak.TxostenEgoera.Zain)
                    ZainZerrenda.Add(t);
                else if (t.Egoera == DatuBasea.Ereduak.TxostenEgoera.Onartua)
                    OnartuaZerrenda.Add(t);
                else if (t.Egoera == DatuBasea.Ereduak.TxostenEgoera.Ukatua)
                    UkatuaZerrenda.Add(t);
                else if (t.Egoera == DatuBasea.Ereduak.TxostenEgoera.Ezeztatua)
                    EzeztatuaZerrenda.Add(t);
            }
        }
        catch (TursoExekuzioSalbuespena libEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                ?? "Datu-base errorea: ezin izan da irakurri. Saiatu berriro.";
            _logger.LogError(libEx, "TxartelKanban: Turso errorea.");
        }
        catch (KeyNotFoundException knfEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ZutabeEskemaMezua;
            _logger.LogError(knfEx, "TxartelKanban: mapa errorea.");
        }
        catch (SQLiteException sqlEx)
        {
            ErroreMezua = "Datu-base errorea: ezin izan da irakurri. Saiatu berriro.";
            _logger.LogError(sqlEx, "TxartelKanban: SQLite errorea.");
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "TxartelKanban: ustekabeko errorea.");
        }
        finally
        {
            IsKargatzean = false;
        }
    }

    [RelayCommand]
    private async Task IkusiXehetasunaAsync(TxostenOnarpenLaburpena txartela)
    {
        if (txartela is null)
            return;
        await Shell.Current.GoToAsync(
            $"{nameof(LangileaTxartelaXehetasunOrria)}?TxostenId={txartela.TxostenId}").ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task EzeztatuTxartela(TxostenOnarpenLaburpena txartela)
    {
        if (txartela is null || !txartela.EzeztatuDaiteke)
            return;

        var berretsi = await _berrespenLeiho.BerretsiAsync(
            "Txartela ezeztatu",
            "Ziur zaude txartel hau ezeztatu nahi duzula?").ConfigureAwait(true);
        if (!berretsi)
            return;

        ErroreMezua = null;
        try
        {
            IsKargatzean = true;
            var erabiltzaileId = await _autorizazioZerbitzua.EskuratuOraingoErabiltzaileIdAsync().ConfigureAwait(true);
            if (erabiltzaileId is null)
            {
                ErroreMezua = "Saioa iraungitu da. Berriz hasi saioa.";
                return;
            }

            await _datuBaseaZerbitzua.EzeztatuTxostenaLangileakAsync(
                txartela.TxostenId, erabiltzaileId.Value).ConfigureAwait(true);

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await BokadilloErakustzailea.SaiatuErakutsiAsync("Txartela ezeztatua.", _logger).ConfigureAwait(true);
            }).ConfigureAwait(true);

            await AgertzenDeneanAsync().ConfigureAwait(true);
        }
        catch (TursoExekuzioSalbuespena libEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                ?? "Datu-base errorea: ezin izan da gorde. Saiatu berriro.";
            _logger.LogError(libEx, "EzeztatuTxartela (Kanban): Turso errorea.");
        }
        catch (SQLiteException sqlEx)
        {
            ErroreMezua = "Datu-base errorea: ezin izan da gorde. Saiatu berriro.";
            _logger.LogError(sqlEx, "EzeztatuTxartela (Kanban): SQLite errorea.");
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "EzeztatuTxartela (Kanban): ustekabeko errorea.");
        }
        finally
        {
            IsKargatzean = false;
        }
    }
}
