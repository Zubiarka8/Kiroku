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
using SQLite;

namespace Kirokuu.ViewModels;

public partial class NireTxartelakViewModel : ObservableObject
{
    public const string IragazkiGuztiak = "Guztiak";

    private readonly DatuBaseaZerbitzua _datuBaseaZerbitzua;
    private readonly AutorizazioZerbitzua _autorizazioZerbitzua;
    private readonly BerrespenLeihoZerbitzua _berrespenLeiho;
    private readonly ILogger<NireTxartelakViewModel> _logger;

    private readonly List<TxostenOnarpenLaburpena> _txartelakGuztiak = new();

    public NireTxartelakViewModel(
        DatuBaseaZerbitzua datuBaseaZerbitzua,
        AutorizazioZerbitzua autorizazioZerbitzua,
        BerrespenLeihoZerbitzua berrespenLeiho,
        ILogger<NireTxartelakViewModel> logger)
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IragazkiaGuztiakHautatua))]
    [NotifyPropertyChangedFor(nameof(IragazkiaZainHautatua))]
    [NotifyPropertyChangedFor(nameof(IragazkiaOnartuaHautatua))]
    [NotifyPropertyChangedFor(nameof(IragazkiaUkatuaHautatua))]
    [NotifyPropertyChangedFor(nameof(IragazkiaEzeztatuaHautatua))]
    private string _egoeraIragazkia = IragazkiGuztiak;

    [ObservableProperty]
    private int _kopuruaGuztiak;

    [ObservableProperty]
    private int _kopuruaZain;

    [ObservableProperty]
    private int _kopuruaOnartua;

    [ObservableProperty]
    private int _kopuruaUkatua;

    [ObservableProperty]
    private int _kopuruaEzeztatua;

    public bool IragazkiaGuztiakHautatua => EgoeraIragazkia == IragazkiGuztiak;
    public bool IragazkiaZainHautatua => EgoeraIragazkia == TxostenEgoera.Zain;
    public bool IragazkiaOnartuaHautatua => EgoeraIragazkia == TxostenEgoera.Onartua;
    public bool IragazkiaUkatuaHautatua => EgoeraIragazkia == TxostenEgoera.Ukatua;
    public bool IragazkiaEzeztatuaHautatua => EgoeraIragazkia == TxostenEgoera.Ezeztatua;

    public ObservableCollection<TxostenOnarpenLaburpena> Txartelak { get; } = new();

    [RelayCommand]
    private async Task AgertzenDeneanAsync()
    {
        if (IsKargatzean)
            return;

        IsKargatzean = true;
        ErroreMezua = null;
        _txartelakGuztiak.Clear();
        Txartelak.Clear();
        BerritzeKopuruak();

        try
        {
            var erabiltzaileId = await _autorizazioZerbitzua.EskuratuOraingoErabiltzaileIdAsync().ConfigureAwait(true);
            if (erabiltzaileId is null)
            {
                ErroreMezua = "Saioa iraungitu da. Berriz hasi saioa.";
                return;
            }

            var zerrenda = await _datuBaseaZerbitzua.ZerrendatuLangilerenTxostenakAsync(erabiltzaileId.Value).ConfigureAwait(true);
            _txartelakGuztiak.AddRange(zerrenda);

            BerritzeKopuruak();
            AplikatuIragazkia();
        }
        catch (TursoExekuzioSalbuespena libEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                ?? "Datu-base errorea: ezin izan da irakurri. Saiatu berriro.";
            _logger.LogError(libEx, "NireTxartelak: Turso errorea.");
        }
        catch (KeyNotFoundException knfEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ZutabeEskemaMezua;
            _logger.LogError(knfEx, "NireTxartelak: mapa errorea.");
        }
        catch (SQLiteException sqlEx)
        {
            ErroreMezua = "Datu-base errorea: ezin izan da irakurri. Saiatu berriro.";
            _logger.LogError(sqlEx, "NireTxartelak: SQLite errorea.");
        }
        catch (HttpRequestException httpEx)
        {
            ErroreMezua = "Sare errorea: konexioa egiaztatu eta saiatu berriro.";
            _logger.LogError(httpEx, "NireTxartelak: sare errorea.");
        }
        catch (TaskCanceledException tcEx)
        {
            ErroreMezua = "Eskaerak denbora muga gainditu du. Saiatu berriro.";
            _logger.LogError(tcEx, "NireTxartelak: denbora muga.");
        }
        catch (Exception ex)
        {
            ErroreMezua = $"Ustekabeko errorea: {ex.GetType().Name} — {ex.Message}";
            _logger.LogError(ex, "NireTxartelak: ustekabeko errorea.");
        }
        finally
        {
            IsKargatzean = false;
        }
    }

    [RelayCommand]
    private void AldatuIragazkia(string? egoera)
    {
        var berria = string.IsNullOrWhiteSpace(egoera) ? IragazkiGuztiak : egoera!;
        if (berria == EgoeraIragazkia)
            return;
        EgoeraIragazkia = berria;
        AplikatuIragazkia();
    }

    private void AplikatuIragazkia()
    {
        Txartelak.Clear();
        IEnumerable<TxostenOnarpenLaburpena> iragazia = EgoeraIragazkia == IragazkiGuztiak
            ? _txartelakGuztiak
            : _txartelakGuztiak.Where(t => string.Equals(t.Egoera, EgoeraIragazkia, StringComparison.Ordinal));
        foreach (var t in iragazia)
            Txartelak.Add(t);
    }

    private void BerritzeKopuruak()
    {
        KopuruaGuztiak = _txartelakGuztiak.Count;
        KopuruaZain = _txartelakGuztiak.Count(t => string.Equals(t.Egoera, TxostenEgoera.Zain, StringComparison.Ordinal));
        KopuruaOnartua = _txartelakGuztiak.Count(t => string.Equals(t.Egoera, TxostenEgoera.Onartua, StringComparison.Ordinal));
        KopuruaUkatua = _txartelakGuztiak.Count(t => string.Equals(t.Egoera, TxostenEgoera.Ukatua, StringComparison.Ordinal));
        KopuruaEzeztatua = _txartelakGuztiak.Count(t => string.Equals(t.Egoera, TxostenEgoera.Ezeztatua, StringComparison.Ordinal));
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

            IsKargatzean = false;
            await AgertzenDeneanAsync().ConfigureAwait(true);
        }
        catch (TursoExekuzioSalbuespena libEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                ?? "Datu-base errorea: ezin izan da gorde. Saiatu berriro.";
            _logger.LogError(libEx, "EzeztatuTxartela: Turso errorea.");
        }
        catch (SQLiteException sqlEx)
        {
            ErroreMezua = "Datu-base errorea: ezin izan da gorde. Saiatu berriro.";
            _logger.LogError(sqlEx, "EzeztatuTxartela: SQLite errorea.");
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "EzeztatuTxartela: ustekabeko errorea.");
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
    private async Task TxartelBerriaSortuAsync()
    {
        await Shell.Current.GoToAsync(nameof(TxartelBerriaOrria)).ConfigureAwait(true);
    }
}
