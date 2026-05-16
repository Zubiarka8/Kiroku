using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.DatuEreduak;
using Kirokuu.Grafikoak;
using Kirokuu.ZerbitzuakSaioa;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Extensions.Logging;

namespace Kirokuu.ViewModels;

public partial class HasieraViewModel : ObservableObject
{
    private readonly SaioaGordetzeZerbitzua _saioaGordetzeZerbitzua;
    private readonly DatuBaseaZerbitzua _datuBaseaZerbitzua;
    private readonly AutorizazioZerbitzua _autorizazioZerbitzua;
    private readonly ILogger<HasieraViewModel> _logger;

    public HasieraViewModel(
        SaioaGordetzeZerbitzua saioaGordetzeZerbitzua,
        DatuBaseaZerbitzua datuBaseaZerbitzua,
        AutorizazioZerbitzua autorizazioZerbitzua,
        ILogger<HasieraViewModel> logger)
    {
        _saioaGordetzeZerbitzua = saioaGordetzeZerbitzua ?? throw new ArgumentNullException(nameof(saioaGordetzeZerbitzua));
        _datuBaseaZerbitzua = datuBaseaZerbitzua ?? throw new ArgumentNullException(nameof(datuBaseaZerbitzua));
        _autorizazioZerbitzua = autorizazioZerbitzua ?? throw new ArgumentNullException(nameof(autorizazioZerbitzua));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        EraikiGrafikoLehenetsiak();
    }

    [ObservableProperty]
    private bool _isKargatzean;

    [ObservableProperty]
    private string? _erroreMezua;

    [ObservableProperty]
    private string _ongietorria = string.Empty;

    [ObservableProperty]
    private string _onartutakoGastuenTestua = string.Empty;

    [ObservableProperty]
    private string _zainTxartelenTestua = string.Empty;

    [ObservableProperty]
    private ISeries[] _hilabetekoGastuSerieak = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _hilabetekoXArdatzak = Array.Empty<Axis>();

    [ObservableProperty]
    private ISeries[] _kategoriaGastuSerieak = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _kategoriaBarraSerieak = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _kategoriaBarraYArdatzak = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _kategoriaBarraXArdatzak = Array.Empty<Axis>();

    [ObservableProperty]
    private bool _erakutsiKategoriaHutsikMezua;

    [ObservableProperty]
    private bool _erakutsiKategoriaBakarra;

    [ObservableProperty]
    private bool _erakutsiKategoriaPieGrafikoa;

    [ObservableProperty]
    private bool _erakutsiKategoriaBarraGrafikoa;

    [ObservableProperty]
    private string _kategoriaBakarXehetasuna = string.Empty;

    [ObservableProperty]
    private ISeries[] _egoeraSerieak = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _egoeraYArdatzak = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _egoeraXArdatzak = Array.Empty<Axis>();

    [RelayCommand]
    private async Task AgertzenDeneanAsync()
    {
        ErroreMezua = null;
        try
        {
            IsKargatzean = true;
            var izenak = await _saioaGordetzeZerbitzua.IrakurriIzenAbizenakAsync().ConfigureAwait(true);
            if (izenak is { } p)
                Ongietorria = $"Ongi etorri, {p.Izena} {p.Abizena}";
            else
                Ongietorria = "Ongi etorri";

            var erabiltzaileId = await _autorizazioZerbitzua.EskuratuOraingoErabiltzaileIdAsync().ConfigureAwait(true);
            if (erabiltzaileId is null)
            {
                ErroreMezua = "Saioa iraungitu da. Berriz hasi saioa.";
                EraikiGrafikoLehenetsiak();
                return;
            }

            var onartutakoGuztira =
                await _datuBaseaZerbitzua.EskuratuOnartutakoGastuenGuztiraLangileAsync(erabiltzaileId.Value).ConfigureAwait(true);
            OnartutakoGastuenTestua =
                $"{onartutakoGuztira.ToString("N2", HasieraGrafikoEraikitzailea.KulturaZenbakietarako)} €";

            var zainKop =
                await _datuBaseaZerbitzua.EskuratuZainTxartenKopuruaLangileAsync(erabiltzaileId.Value).ConfigureAwait(true);
            ZainTxartelenTestua = zainKop.ToString(HasieraGrafikoEraikitzailea.KulturaZenbakietarako);

            var hilabetekoak = await _datuBaseaZerbitzua
                .EskuratuAzkenHilabeteetakoOnartutakoGastuakLangileAsync(erabiltzaileId.Value, HasieraGrafikoEraikitzailea.HilabeteKopuruaGrafikoan)
                .ConfigureAwait(true);
            EraikiHilabetekoGrafikoa(hilabetekoak);

            var kategoriak = await _datuBaseaZerbitzua
                .EskuratuKategoriakoOnartutakoGastuakLangileAsync(erabiltzaileId.Value)
                .ConfigureAwait(true);
            EraikiKategoriaGrafikoa(kategoriak);

            var egoerak = await _datuBaseaZerbitzua
                .EskuratuTxostenKopuruakEgoerarenAraberaLangileAsync(erabiltzaileId.Value)
                .ConfigureAwait(true);
            EraikiEgoeraGrafikoa(egoerak);
        }
        catch (Exception ex)
        {
            ViewModelSalbuespenTratatzailea.TratatuIrakurketa(ex, m => ErroreMezua = m, _logger, "Hasiera");
            EraikiGrafikoLehenetsiak();
        }
        finally
        {
            IsKargatzean = false;
        }
    }

    private void EraikiGrafikoLehenetsiak()
    {
        HilabetekoGastuSerieak = Array.Empty<ISeries>();
        HilabetekoXArdatzak = Array.Empty<Axis>();
        GarbituKategoriaGrafikoEgoera();
        EgoeraSerieak = Array.Empty<ISeries>();
        EgoeraYArdatzak = Array.Empty<Axis>();
        EgoeraXArdatzak = Array.Empty<Axis>();
    }

    private void EraikiHilabetekoGrafikoa(IReadOnlyList<HilabetekoGastuAgregatua> datuak)
    {
        HasieraGrafikoEraikitzailea.EraikiHilabetekoGrafikoa(
            datuak,
            out var serieak,
            out var xArdatzak);
        HilabetekoGastuSerieak = serieak;
        HilabetekoXArdatzak = xArdatzak;
    }

    private void GarbituKategoriaGrafikoEgoera() =>
        AplikatuKategoriaGrafikoEmaitza(HasieraGrafikoEraikitzailea.GarbituKategoriaGrafikoa());

    private void EraikiKategoriaGrafikoa(IReadOnlyList<KategoriakoGastuAgregatua> datuak) =>
        AplikatuKategoriaGrafikoEmaitza(HasieraGrafikoEraikitzailea.EraikiKategoriaGrafikoa(datuak));

    private void AplikatuKategoriaGrafikoEmaitza(HasieraGrafikoEraikitzailea.KategoriaGrafikoEmaitza emaitza)
    {
        KategoriaGastuSerieak = emaitza.GastuSerieak;
        KategoriaBarraSerieak = emaitza.BarraSerieak;
        KategoriaBarraYArdatzak = emaitza.BarraYArdatzak;
        KategoriaBarraXArdatzak = emaitza.BarraXArdatzak;
        ErakutsiKategoriaHutsikMezua = emaitza.ErakutsiHutsikMezua;
        ErakutsiKategoriaBakarra = emaitza.ErakutsiBakarra;
        ErakutsiKategoriaPieGrafikoa = emaitza.ErakutsiPieGrafikoa;
        ErakutsiKategoriaBarraGrafikoa = emaitza.ErakutsiBarraGrafikoa;
        KategoriaBakarXehetasuna = emaitza.BakarXehetasuna;
    }

    private void EraikiEgoeraGrafikoa(IReadOnlyList<TxostenEgoeraKopurua> datuak)
    {
        HasieraGrafikoEraikitzailea.EraikiEgoeraGrafikoa(datuak, out var serieak, out var y, out var x);
        EgoeraSerieak = serieak;
        EgoeraYArdatzak = y;
        EgoeraXArdatzak = x;
    }
}
