using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.DatuEreduak;
using Kirokuu.Pages;
using Kirokuu.Zerbitzuak;
using Kirokuu.ZerbitzuakSaioa;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using SQLite;
using System.Net.Http;

namespace Kirokuu.ViewModels;

public partial class TxostenGuztiekViewModel : ObservableObject
{
    private readonly AutorizazioZerbitzua _autorizazioZerbitzua;
    private readonly DatuBaseaZerbitzua _datuBaseaZerbitzua;
    private readonly ILogger<TxostenGuztiekViewModel> _logger;
    private string? _adminSektoreIzenaGordeta;

    public TxostenGuztiekViewModel(
        AutorizazioZerbitzua autorizazioZerbitzua,
        DatuBaseaZerbitzua datuBaseaZerbitzua,
        ILogger<TxostenGuztiekViewModel> logger)
    {
        _autorizazioZerbitzua = autorizazioZerbitzua ?? throw new ArgumentNullException(nameof(autorizazioZerbitzua));
        _datuBaseaZerbitzua = datuBaseaZerbitzua ?? throw new ArgumentNullException(nameof(datuBaseaZerbitzua));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [ObservableProperty]
    private bool _isKargatzean;

    [ObservableProperty]
    private bool _isBerritzen;

    [ObservableProperty]
    private string? _erroreMezua;

    public ObservableCollection<TxostenOnarpenLaburpena> TxostenGuztiak { get; } = new();

    [ObservableProperty]
    private TxostenOnarpenLaburpena? _selektatutakoLaburpena;

    // Lehenetsia: Guztiak (null), CEO/admin-ek aukeratzen du chip bidez.
    [ObservableProperty]
    private string? _egoeraFiltroa;

    [ObservableProperty]
    private int _kopuruGuztiak;

    [ObservableProperty]
    private int _kopuruZain;

    [ObservableProperty]
    private int _kopuruOnartua;

    [ObservableProperty]
    private int _kopuruUkatua;

    [ObservableProperty]
    private int _kopuruEzeztatua;

    public bool DaGuztiakIragazkia => string.IsNullOrEmpty(EgoeraFiltroa);

    partial void OnEgoeraFiltroaChanged(string? value) => OnPropertyChanged(nameof(DaGuztiakIragazkia));

    partial void OnSelektatutakoLaburpenaChanged(TxostenOnarpenLaburpena? value)
    {
        if (value is null)
            return;
        var hautatua = value;
        SelektatutakoLaburpena = null;
        _ = IrekiTxostenXehetasunaAsync(hautatua);
    }

    [RelayCommand]
    private async Task HautatuFiltroaAsync(string? egoera)
    {
        EgoeraFiltroa = string.Equals(EgoeraFiltroa, egoera, StringComparison.Ordinal) ? null : egoera;
        await ZerrendaKargatuAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task IrekiTxostenXehetasunaAsync(TxostenOnarpenLaburpena? laburpena)
    {
        if (laburpena is null || laburpena.TxostenId <= 0)
            return;

        try
        {
            await Shell.Current
                .GoToAsync($"{nameof(TxostenOnarpenXehetasunOrria)}?TxostenId={laburpena.TxostenId}")
                .ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TxostenGuztiak: nabigazio errorea.");
            ErroreMezua = $"Ezin izan da xehetasuna ireki: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task AgertzenDeneanAsync()
    {
        ErroreMezua = null;
        try
        {
            IsKargatzean = true;
            if (!await _autorizazioZerbitzua.DaAdministratzaileaAsync().ConfigureAwait(true))
            {
                ErroreMezua = "Ez duzu baimenik atal honetan.";
                return;
            }

            var adminId = await _autorizazioZerbitzua.EskuratuOraingoErabiltzaileIdAsync().ConfigureAwait(true);
            _adminSektoreIzenaGordeta = null;
            if (adminId is { } aid)
            {
                var admin = await _datuBaseaZerbitzua.BilatuErabiltzaileaIdzAsync(aid).ConfigureAwait(true);
                var sektoreIzena = admin?.Sektorea;
                if (!string.IsNullOrWhiteSpace(sektoreIzena))
                    _adminSektoreIzenaGordeta = sektoreIzena;
            }

            await ZerrendaKargatuAsync().ConfigureAwait(true);
        }
        catch (TursoExekuzioSalbuespena libEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                ?? "Datu-base errorea: ezin izan da irakurri.";
            _logger.LogError(libEx, "TxostenGuztiak: Turso errorea.");
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "TxostenGuztiak: ustekabeko errorea.");
        }
        finally
        {
            IsKargatzean = false;
            IsBerritzen = false;
        }
    }

    private async Task ZerrendaKargatuAsync()
    {
        try
        {
            IsKargatzean = true;
            ErroreMezua = null;
            TxostenGuztiak.Clear();

            var txostenak = await _datuBaseaZerbitzua
                .ZerrendatuTxostenGuztiekAsync(_adminSektoreIzenaGordeta, EgoeraFiltroa)
                .ConfigureAwait(true);
            foreach (var t in txostenak)
                TxostenGuztiak.Add(t);

            await KopuruakEguneratuAsync().ConfigureAwait(true);
        }
        catch (TursoExekuzioSalbuespena libEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                ?? "Datu-base errorea: ezin izan da irakurri.";
            _logger.LogError(libEx, "TxostenGuztiak: ZerrendaKargatu Turso errorea.");
        }
        catch (HttpRequestException httpEx)
        {
            ErroreMezua = "Sare errorea: konexioa egiaztatu eta saiatu berriro.";
            _logger.LogError(httpEx, "TxostenGuztiak: sare errorea.");
        }
        catch (TaskCanceledException)
        {
            ErroreMezua = "Eskaerak denbora muga gainditu du. Saiatu berriro.";
        }
        catch (SQLiteException sqlEx)
        {
            ErroreMezua = "Datu-base errorea: ezin izan da irakurri. Saiatu berriro.";
            _logger.LogError(sqlEx, "TxostenGuztiak: SQLite errorea.");
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da.";
            _logger.LogError(ex, "TxostenGuztiak: ZerrendaKargatu ustekabeko errorea.");
        }
        finally
        {
            IsKargatzean = false;
        }
    }

    private async Task KopuruakEguneratuAsync()
    {
        var kopuruak = await _datuBaseaZerbitzua
            .EskuratuTxostenKopuruakEgoerarenAraberaSektoreanAsync(_adminSektoreIzenaGordeta)
            .ConfigureAwait(true);

        KopuruZain = KopuruEgoeraz(kopuruak, TxostenEgoera.Zain);
        KopuruOnartua = KopuruEgoeraz(kopuruak, TxostenEgoera.Onartua);
        KopuruUkatua = KopuruEgoeraz(kopuruak, TxostenEgoera.Ukatua);
        KopuruEzeztatua = KopuruEgoeraz(kopuruak, TxostenEgoera.Ezeztatua);
        KopuruGuztiak = KopuruZain + KopuruOnartua + KopuruUkatua + KopuruEzeztatua;
    }

    private static int KopuruEgoeraz(IReadOnlyList<TxostenEgoeraKopurua> zerrenda, string egoera) =>
        zerrenda.FirstOrDefault(k => string.Equals(k.Egoera, egoera, StringComparison.Ordinal))?.Kopurua ?? 0;
}
