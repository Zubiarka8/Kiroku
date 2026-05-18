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

public partial class NireTxartelakViewModel : ObservableObject
{
    public const string IragazkiGuztiak = "Guztiak";

    private readonly DatuBaseaZerbitzua _datuBaseaZerbitzua;
    private readonly AutorizazioZerbitzua _autorizazioZerbitzua;
    private readonly BerrespenLeihoZerbitzua _berrespenLeiho;
    private readonly ILogger<NireTxartelakViewModel> _logger;
    private int? _erabiltzaileIdGordeta;

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

    [ObservableProperty]
    private int _txartelKopuruaErakusten;

    [ObservableProperty]
    private double _zenbatekoGuztiraErakusten;

    [ObservableProperty]
    private string _laburpenGoiburua = string.Empty;

    public bool IragazkiaGuztiakHautatua => EgoeraIragazkia == IragazkiGuztiak;
    public bool IragazkiaZainHautatua => EgoeraIragazkia == TxostenEgoera.Zain;
    public bool IragazkiaOnartuaHautatua => EgoeraIragazkia == TxostenEgoera.Onartua;
    public bool IragazkiaUkatuaHautatua => EgoeraIragazkia == TxostenEgoera.Ukatua;
    public bool IragazkiaEzeztatuaHautatua => EgoeraIragazkia == TxostenEgoera.Ezeztatua;

    public ObservableCollection<TxostenOnarpenLaburpena> Txartelak { get; } = new();

    // Lehenetsia: Guztiak (null), langileak chip bidez iragazten du.
    [ObservableProperty]
    private string? _egoeraFiltroa;

    [ObservableProperty]
    private int _kopuruZain;

    [ObservableProperty]
    private int _kopuruOnartua;

    [ObservableProperty]
    private int _kopuruUkatua;

    [ObservableProperty]
    private int _kopuruEzeztatua;

    [RelayCommand]
    private async Task HautatuFiltroaAsync(string? egoera)
    {
        EgoeraFiltroa = string.Equals(EgoeraFiltroa, egoera, StringComparison.Ordinal) ? null : egoera;
        await ZerrendaKargatuAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task AgertzenDeneanAsync()
    {
        if (IsKargatzean)
            return;

        IsKargatzean = true;
        ErroreMezua = null;
        try
        {
            var erabiltzaileId = await _autorizazioZerbitzua.EskuratuOraingoErabiltzaileIdAsync().ConfigureAwait(true);
            if (erabiltzaileId is null)
            {
                ErroreMezua = "Saioa iraungitu da. Berriz hasi saioa.";
                return;
            }
            _erabiltzaileIdGordeta = erabiltzaileId.Value;

            await ZerrendaKargatuAsync().ConfigureAwait(true);
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
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "NireTxartelak: ustekabeko errorea.");
        }
        finally
        {
            IsKargatzean = false;
        }
    }

    private async Task ZerrendaKargatuAsync()
    {
        if (_erabiltzaileIdGordeta is not { } uid)
            return;

        try
        {
            IsKargatzean = true;
            ErroreMezua = null;
            Txartelak.Clear();

            var zerrenda = await _datuBaseaZerbitzua
                .ZerrendatuLangilerenTxostenakAsync(uid, EgoeraFiltroa)
                .ConfigureAwait(true);
            foreach (var t in zerrenda)
                Txartelak.Add(t);

            await KopuruakEguneratuAsync().ConfigureAwait(true);
        }
        catch (TursoExekuzioSalbuespena libEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                ?? "Datu-base errorea: ezin izan da irakurri.";
            _logger.LogError(libEx, "NireTxartelak: ZerrendaKargatu Turso errorea.");
        }
        catch (HttpRequestException httpEx)
        {
            ErroreMezua = "Sare errorea: konexioa egiaztatu eta saiatu berriro.";
            _logger.LogError(httpEx, "NireTxartelak: sare errorea.");
        }
        catch (TaskCanceledException)
        {
            ErroreMezua = "Eskaerak denbora muga gainditu du. Saiatu berriro.";
        }
        catch (SQLiteException sqlEx)
        {
            ErroreMezua = "Datu-base errorea: ezin izan da irakurri. Saiatu berriro.";
            _logger.LogError(sqlEx, "NireTxartelak: SQLite errorea.");
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da.";
            _logger.LogError(ex, "NireTxartelak: ZerrendaKargatu ustekabeko errorea.");
        }
        finally
        {
            IsKargatzean = false;
        }
    }

    private async Task KopuruakEguneratuAsync()
    {
        if (_erabiltzaileIdGordeta is not { } uid)
            return;
        var kopuruak = await _datuBaseaZerbitzua
            .EskuratuTxostenKopuruakEgoerarenAraberaLangileAsync(uid)
            .ConfigureAwait(true);

        KopuruZain = KopuruEgoeraz(kopuruak, TxostenEgoera.Zain);
        KopuruOnartua = KopuruEgoeraz(kopuruak, TxostenEgoera.Onartua);
        KopuruUkatua = KopuruEgoeraz(kopuruak, TxostenEgoera.Ukatua);
        KopuruEzeztatua = KopuruEgoeraz(kopuruak, TxostenEgoera.Ezeztatua);
    }

    private static int KopuruEgoeraz(IReadOnlyList<TxostenEgoeraKopurua> zerrenda, string egoera) =>
        zerrenda.FirstOrDefault(k => string.Equals(k.Egoera, egoera, StringComparison.Ordinal))?.Kopurua ?? 0;

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

        var zerrenda = iragazia.ToList();
        foreach (var t in zerrenda)
            Txartelak.Add(t);

        TxartelKopuruaErakusten = zerrenda.Count;
        ZenbatekoGuztiraErakusten = zerrenda.Sum(t => t.GastuenBatuketakoZenbatekoa);
        LaburpenGoiburua = TxartelKopuruaErakusten switch
        {
            0 => "Ez dago txartelik iragazki honetan",
            1 => "1 txartel",
            _ => $"{TxartelKopuruaErakusten} txartel"
        };
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

            await ZerrendaKargatuAsync().ConfigureAwait(true);
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
