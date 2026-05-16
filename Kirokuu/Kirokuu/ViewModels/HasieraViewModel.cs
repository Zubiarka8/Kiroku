using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.DatuEreduak;
using Kirokuu.Zerbitzuak;
using Kirokuu.ZerbitzuakSaioa;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using SQLite;

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
        catch (InvalidOperationException opEx)
        {
            ErroreMezua = "Eragiketa baliogabea. Berriz saiatu saioa hasita.";
            _logger.LogError(opEx, "Hasiera: baliogabeko eragiketa.");
            EraikiGrafikoLehenetsiak();
        }
        catch (TursoExekuzioSalbuespena libEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                ?? "Datu-base errorea: ezin izan da kargatu. Saiatu berriro.";
            _logger.LogError(libEx, "Hasiera: Turso errorea.");
            EraikiGrafikoLehenetsiak();
        }
        catch (KeyNotFoundException knfEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ZutabeEskemaMezua;
            _logger.LogError(knfEx, "Hasiera: mapa errorea.");
            EraikiGrafikoLehenetsiak();
        }
        catch (FormatException fmtEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.BalioFormatuMezua;
            _logger.LogError(fmtEx, "Hasiera: formatu errorea.");
            EraikiGrafikoLehenetsiak();
        }
        catch (SQLiteException sqlEx)
        {
            ErroreMezua = "Datu-base errorea: ezin izan da irakurri. Saiatu berriro.";
            _logger.LogError(sqlEx, "Hasiera: SQLite errorea.");
            EraikiGrafikoLehenetsiak();
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "Hasiera: ustekabeko errorea.");
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

    private static string MoztuTestuaElipsis(string? testua, int gehienezkoLuzera) =>
        HasieraGrafikoEraikitzailea.MoztuTestuaElipsis(testua, gehienezkoLuzera);

    private void GarbituKategoriaGrafikoEgoera()
    {
        KategoriaGastuSerieak = Array.Empty<ISeries>();
        KategoriaBarraSerieak = Array.Empty<ISeries>();
        KategoriaBarraYArdatzak = Array.Empty<Axis>();
        KategoriaBarraXArdatzak = Array.Empty<Axis>();
        ErakutsiKategoriaHutsikMezua = false;
        ErakutsiKategoriaBakarra = false;
        ErakutsiKategoriaPieGrafikoa = false;
        ErakutsiKategoriaBarraGrafikoa = false;
        KategoriaBakarXehetasuna = string.Empty;
    }

    private void EraikiKategoriaGrafikoa(IReadOnlyList<KategoriakoGastuAgregatua> datuak)
    {
        GarbituKategoriaGrafikoEgoera();

        var moztuta = datuak.Take(HasieraGrafikoEraikitzailea.KategoriaGehienezkoKopuruaGrafikoan).ToList();
        if (moztuta.Count == 0)
        {
            ErakutsiKategoriaHutsikMezua = true;
            return;
        }

        if (moztuta.Count == 1)
        {
            ErakutsiKategoriaBakarra = true;
            var d = moztuta[0];
            var izena = string.IsNullOrWhiteSpace(d.KontzeptuIzena) ? "?" : d.KontzeptuIzena.Trim();
            KategoriaBakarXehetasuna =
                $"{izena}: {d.Guztira.ToString("N2", HasieraGrafikoEraikitzailea.KulturaZenbakietarako)} € (100%)";
            return;
        }

        if (moztuta.Count >= HasieraGrafikoEraikitzailea.KategoriaBarraGutxienezkoZatituak)
        {
            ErakutsiKategoriaBarraGrafikoa = true;
            EraikiKategoriaBarraGrafikoa(moztuta);
            return;
        }

        ErakutsiKategoriaPieGrafikoa = true;
        EraikiKategoriaPieZatiak(moztuta);
    }

    private void EraikiKategoriaPieZatiak(IReadOnlyList<KategoriakoGastuAgregatua> moztuta)
    {
        var guztira = moztuta.Sum(d => d.Guztira);
        var serieak = new List<ISeries>(moztuta.Count);

        for (var i = 0; i < moztuta.Count; i++)
        {
            var d = moztuta[i];
            var zenbatekoa = d.Guztira;
            var izenaOsoa = string.IsNullOrWhiteSpace(d.KontzeptuIzena) ? "?" : d.KontzeptuIzena.Trim();
            var ehunekoa = guztira > 0 ? zenbatekoa / guztira * 100.0 : 0;
            var legendaTestua =
                $"{MoztuTestuaElipsis(izenaOsoa, HasieraGrafikoEraikitzailea.KategoriaLegendokoIzenGehienezkoLuzera)} · " +
                $"{zenbatekoa.ToString("N2", HasieraGrafikoEraikitzailea.KulturaZenbakietarako)} € · {ehunekoa:0}%";
            var kolorea = HasieraGrafikoEraikitzailea.KategoriaKoloreak[i % HasieraGrafikoEraikitzailea.KategoriaKoloreak.Length];
            var izKopia = izenaOsoa;
            var zbKopia = zenbatekoa;
            var ehKopia = ehunekoa;
            serieak.Add(new PieSeries<double>
            {
                Name = legendaTestua,
                Values = new[] { zenbatekoa },
                InnerRadius = 62,
                Fill = new SolidColorPaint(kolorea),
                DataLabelsPosition = PolarLabelsPosition.Middle,
                DataLabelsSize = 12,
                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                DataLabelsFormatter = pu =>
                {
                    var z = pu.Coordinate.PrimaryValue;
                    var pct = guztira > 0 ? z / guztira * 100.0 : 0;
                    return $"{pct:0}%";
                },
                ToolTipLabelFormatter = _ =>
                    $"{izKopia}: {zbKopia.ToString("N2", HasieraGrafikoEraikitzailea.KulturaZenbakietarako)} € ({ehKopia:0}%)"
            });
        }

        KategoriaGastuSerieak = serieak.ToArray();
    }

    private void EraikiKategoriaBarraGrafikoa(IReadOnlyList<KategoriakoGastuAgregatua> moztuta)
    {
        var balioak = moztuta.Select(d => d.Guztira).ToArray();
        var etiketak = moztuta
            .Select(d => string.IsNullOrWhiteSpace(d.KontzeptuIzena) ? "?" : d.KontzeptuIzena.Trim())
            .ToArray();
        var guztira = balioak.Sum();

        KategoriaBarraSerieak = new ISeries[]
        {
            new RowSeries<double>
            {
                Name = "Onartutako gastua",
                Values = balioak,
                Fill = new SolidColorPaint(SKColors.SteelBlue),
                MaxBarWidth = 20,
                Rx = 4,
                Ry = 4,
                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                DataLabelsSize = 11,
                DataLabelsPosition = DataLabelsPosition.Middle,
                DataLabelsFormatter = pu =>
                {
                    var i = pu.Index;
                    var z = pu.Coordinate.PrimaryValue;
                    var pct = guztira > 0 ? z / guztira * 100.0 : 0;
                    return $"{z.ToString("N0", HasieraGrafikoEraikitzailea.KulturaZenbakietarako)} € · {pct:0}%";
                }
            }
        };

        KategoriaBarraYArdatzak = new Axis[]
        {
            new Axis
            {
                Labels = etiketak,
                LabelsRotation = 0,
                ForceStepToMin = true,
                MinStep = 1,
                IsInverted = true,
                SeparatorsPaint = new SolidColorPaint(new SKColor(210, 210, 210)),
                TextSize = 11
            }
        };

        KategoriaBarraXArdatzak = new Axis[]
        {
            new Axis
            {
                MinLimit = 0,
                Labeler = v => $"{v.ToString("N0", HasieraGrafikoEraikitzailea.KulturaZenbakietarako)} €"
            }
        };
    }

    private void EraikiEgoeraGrafikoa(IReadOnlyList<TxostenEgoeraKopurua> datuak)
    {
        HasieraGrafikoEraikitzailea.EraikiEgoeraGrafikoa(datuak, out var serieak, out var y, out var x);
        EgoeraSerieak = serieak;
        EgoeraYArdatzak = y;
        EgoeraXArdatzak = x;
    }
}
