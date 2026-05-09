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

public partial class AdministratzaileHasieraViewModel : ObservableObject
{
    private const int HilabeteKopuruaGrafikoan = 6;

    private const int KategoriaGehienezkoKopuruaGrafikoan = 8;

    private const int KategoriaBarraGutxienezkoZatituak = 6;

    private const int KategoriaLegendokoIzenGehienezkoLuzera = 38;

    private static readonly CultureInfo KulturaZenbakietarako = CultureInfo.GetCultureInfo("eu-ES");

    private static readonly SKColor[] KategoriaKoloreak =
    {
        new(33, 150, 243),
        new(255, 152, 0),
        new(76, 175, 80),
        new(171, 71, 188),
        new(236, 64, 122),
        new(0, 150, 136),
        new(255, 193, 7),
        new(121, 85, 72)
    };

    private readonly DatuBaseaZerbitzua _datuBaseaZerbitzua;
    private readonly ILogger<AdministratzaileHasieraViewModel> _logger;

    public AdministratzaileHasieraViewModel(
        DatuBaseaZerbitzua datuBaseaZerbitzua,
        ILogger<AdministratzaileHasieraViewModel> logger)
    {
        _datuBaseaZerbitzua = datuBaseaZerbitzua ?? throw new ArgumentNullException(nameof(datuBaseaZerbitzua));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        EraikiGrafikoLehenetsiak();
    }

    [ObservableProperty]
    private bool _isKargatzean;

    [ObservableProperty]
    private string? _erroreMezua;

    [ObservableProperty]
    private string _laburpenBurua = "Organizazioaren laburpena";

    [ObservableProperty]
    private string _onartutakoGastuenTestua = string.Empty;

    [ObservableProperty]
    private string _zainTxartelenTestua = string.Empty;

    [ObservableProperty]
    private string _langileKopuruTestua = string.Empty;

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

            var langileak = await _datuBaseaZerbitzua.ZerrendatuLangileLaburpenakAsync().ConfigureAwait(true);
            LangileKopuruTestua = langileak.Count.ToString(KulturaZenbakietarako);

            var onartutakoGuztira =
                await _datuBaseaZerbitzua.EskuratuOnartutakoGastuenGuztiraOrguOrokorraAsync().ConfigureAwait(true);
            OnartutakoGastuenTestua =
                $"{onartutakoGuztira.ToString("N2", KulturaZenbakietarako)} €";

            var zainKop =
                await _datuBaseaZerbitzua.EskuratuZainTxartenKopuruaOrguOrokorraAsync().ConfigureAwait(true);
            ZainTxartelenTestua = zainKop.ToString(KulturaZenbakietarako);

            var hilabetekoak = await _datuBaseaZerbitzua
                .EskuratuAzkenHilabeteetakoOnartutakoGastuakOrguOrokorraAsync(HilabeteKopuruaGrafikoan)
                .ConfigureAwait(true);
            EraikiHilabetekoGrafikoa(hilabetekoak);

            var kategoriak = await _datuBaseaZerbitzua.EskuratuKategoriakoOnartutakoGastuakOrguOrokorraAsync().ConfigureAwait(true);
            EraikiKategoriaGrafikoa(kategoriak);

            var egoerak = await _datuBaseaZerbitzua.EskuratuTxostenKopuruakEgoerarenAraberaOrguOrokorraAsync().ConfigureAwait(true);
            EraikiEgoeraGrafikoa(egoerak);
        }
        catch (TursoExekuzioSalbuespena libEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                ?? "Datu-base errorea: ezin izan da kargatu. Saiatu berriro.";
            _logger.LogError(libEx, "AdministratzaileHasiera: Turso errorea.");
            EraikiGrafikoLehenetsiak();
        }
        catch (KeyNotFoundException knfEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ZutabeEskemaMezua;
            _logger.LogError(knfEx, "AdministratzaileHasiera: mapa errorea.");
            EraikiGrafikoLehenetsiak();
        }
        catch (FormatException fmtEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.BalioFormatuMezua;
            _logger.LogError(fmtEx, "AdministratzaileHasiera: formatu errorea.");
            EraikiGrafikoLehenetsiak();
        }
        catch (SQLiteException sqlEx)
        {
            ErroreMezua = "Datu-base errorea: ezin izan da irakurri. Saiatu berriro.";
            _logger.LogError(sqlEx, "AdministratzaileHasiera: SQLite errorea.");
            EraikiGrafikoLehenetsiak();
        }
        catch (InvalidOperationException opEx)
        {
            ErroreMezua = "Eragiketa baliogabea. Berriz saiatu saioa hasita.";
            _logger.LogError(opEx, "AdministratzaileHasiera: baliogabeko eragiketa.");
            EraikiGrafikoLehenetsiak();
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "AdministratzaileHasiera: ustekabeko errorea.");
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
        var balioak = datuak.Select(d => d.Guztira).ToArray();
        var etiketak = datuak.Select(d => d.Hilabetea).ToArray();
        var lerroKolorea = new SolidColorPaint(SKColors.RoyalBlue) { StrokeThickness = 2.5f };
        var eremuKolorea = new SolidColorPaint(SKColors.RoyalBlue.WithAlpha(88));
        HilabetekoGastuSerieak = new ISeries[]
        {
            new LineSeries<double>
            {
                Name = "Onartutako gastua (org.)",
                Values = balioak,
                Fill = eremuKolorea,
                Stroke = lerroKolorea,
                GeometryFill = new SolidColorPaint(SKColors.White),
                GeometryStroke = lerroKolorea,
                GeometrySize = 7,
                LineSmoothness = 0.35
            }
        };
        HilabetekoXArdatzak = new Axis[]
        {
            new Axis
            {
                Labels = etiketak,
                LabelsRotation = -35,
                ForceStepToMin = true,
                MinStep = 1,
                SeparatorsPaint = new SolidColorPaint(new SKColor(210, 210, 210)),
                TicksPaint = new SolidColorPaint(new SKColor(35, 35, 35))
            }
        };
    }

    private static string MoztuTestuaElipsis(string? testua, int gehienezkoLuzera)
    {
        if (string.IsNullOrWhiteSpace(testua))
            return "?";
        var t = testua.Trim();
        if (t.Length <= gehienezkoLuzera)
            return t;
        return t[..(gehienezkoLuzera - 1)] + "…";
    }

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

        var moztuta = datuak.Take(KategoriaGehienezkoKopuruaGrafikoan).ToList();
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
                $"{izena}: {d.Guztira.ToString("N2", KulturaZenbakietarako)} € (100%)";
            return;
        }

        if (moztuta.Count >= KategoriaBarraGutxienezkoZatituak)
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
                $"{MoztuTestuaElipsis(izenaOsoa, KategoriaLegendokoIzenGehienezkoLuzera)} · " +
                $"{zenbatekoa.ToString("N2", KulturaZenbakietarako)} € · {ehunekoa:0}%";

            var kolorea = KategoriaKoloreak[i % KategoriaKoloreak.Length];
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
                    $"{izKopia}: {zbKopia.ToString("N2", KulturaZenbakietarako)} € ({ehKopia:0}%)"
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
                Name = "Onartutako gastua (org.)",
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
                    var z = pu.Coordinate.PrimaryValue;
                    var pct = guztira > 0 ? z / guztira * 100.0 : 0;
                    return $"{z.ToString("N0", KulturaZenbakietarako)} € · {pct:0}%";
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
                Labeler = v => $"{v.ToString("N0", KulturaZenbakietarako)} €"
            }
        };
    }

    private void EraikiEgoeraGrafikoa(IReadOnlyList<TxostenEgoeraKopurua> datuak)
    {
        var mapa = datuak.ToDictionary(x => x.Egoera, x => x.Kopurua, StringComparer.Ordinal);
        var ordena = new[]
        {
            TxostenEgoera.Zain,
            TxostenEgoera.Onartua,
            TxostenEgoera.Ukatua,
            TxostenEgoera.Ezeztatua
        };
        var balioak = ordena.Select(e => (double)(mapa.TryGetValue(e, out var k) ? k : 0)).ToArray();
        EgoeraSerieak = new ISeries[]
        {
            new RowSeries<double>
            {
                Name = "Txartel kopurua (org.)",
                Values = balioak,
                Fill = new SolidColorPaint(SKColors.DarkOrange),
                MaxBarWidth = 22,
                Rx = 4,
                Ry = 4,
                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                DataLabelsSize = 13,
                DataLabelsPosition = DataLabelsPosition.End
            }
        };
        EgoeraYArdatzak = new Axis[]
        {
            new Axis
            {
                Labels = ordena,
                ForceStepToMin = true,
                MinStep = 1,
                SeparatorsPaint = new SolidColorPaint(new SKColor(210, 210, 210))
            }
        };
        EgoeraXArdatzak = new Axis[]
        {
            new Axis { MinLimit = 0 }
        };
    }
}
