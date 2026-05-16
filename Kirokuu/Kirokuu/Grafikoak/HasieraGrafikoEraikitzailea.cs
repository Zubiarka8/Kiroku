using System.Globalization;
using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.DatuEreduak;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace Kirokuu.Grafikoak;

public static class HasieraGrafikoEraikitzailea
{
    public const int HilabeteKopuruaGrafikoan = 6;

    public const int KategoriaGehienezkoKopuruaGrafikoan = 8;

    public const int KategoriaBarraGutxienezkoZatituak = 6;

    public const int KategoriaLegendokoIzenGehienezkoLuzera = 38;

    public static readonly CultureInfo KulturaZenbakietarako = CultureInfo.GetCultureInfo("eu-ES");

    public static readonly SKColor[] KategoriaKoloreak =
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

    public static string MoztuTestuaElipsis(string? testua, int gehienezkoLuzera)
    {
        if (string.IsNullOrWhiteSpace(testua))
            return "?";
        var t = testua.Trim();
        if (t.Length <= gehienezkoLuzera)
            return t;
        return t[..(gehienezkoLuzera - 1)] + "…";
    }

    public static void EraikiHilabetekoGrafikoa(
        IReadOnlyList<HilabetekoGastuAgregatua> datuak,
        out ISeries[] serieak,
        out Axis[] xArdatzak,
        string serieIzena = "Onartutako gastua")
    {
        var balioak = datuak.Select(d => d.Guztira).ToArray();
        var etiketak = datuak.Select(d => d.Hilabetea).ToArray();
        var lerroKolorea = new SolidColorPaint(SKColors.DodgerBlue) { StrokeThickness = 2.5f };
        var eremuKolorea = new SolidColorPaint(SKColors.DodgerBlue.WithAlpha(90));
        serieak = new ISeries[]
        {
            new LineSeries<double>
            {
                Name = serieIzena,
                Values = balioak,
                Fill = eremuKolorea,
                Stroke = lerroKolorea,
                GeometryFill = new SolidColorPaint(SKColors.White),
                GeometryStroke = lerroKolorea,
                GeometrySize = 7,
                LineSmoothness = 0.35
            }
        };
        xArdatzak = new Axis[]
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

    public static void EraikiEgoeraGrafikoa(
        IReadOnlyList<TxostenEgoeraKopurua> datuak,
        out ISeries[] serieak,
        out Axis[] yArdatzak,
        out Axis[] xArdatzak)
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
        serieak = new ISeries[]
        {
            new RowSeries<double>
            {
                Name = "Txartel kopurua",
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
        yArdatzak = new Axis[]
        {
            new Axis
            {
                Labels = ordena,
                ForceStepToMin = true,
                MinStep = 1,
                SeparatorsPaint = new SolidColorPaint(new SKColor(210, 210, 210))
            }
        };
        xArdatzak = new Axis[] { new Axis { MinLimit = 0 } };
    }

    public sealed class KategoriaGrafikoEmaitza
    {
        public ISeries[] GastuSerieak { get; init; } = Array.Empty<ISeries>();

        public ISeries[] BarraSerieak { get; init; } = Array.Empty<ISeries>();

        public Axis[] BarraYArdatzak { get; init; } = Array.Empty<Axis>();

        public Axis[] BarraXArdatzak { get; init; } = Array.Empty<Axis>();

        public bool ErakutsiHutsikMezua { get; init; }

        public bool ErakutsiBakarra { get; init; }

        public bool ErakutsiPieGrafikoa { get; init; }

        public bool ErakutsiBarraGrafikoa { get; init; }

        public string BakarXehetasuna { get; init; } = string.Empty;
    }

    public static KategoriaGrafikoEmaitza GarbituKategoriaGrafikoa() => new();

    public static KategoriaGrafikoEmaitza EraikiKategoriaGrafikoa(
        IReadOnlyList<KategoriakoGastuAgregatua> datuak,
        string barraSerieIzena = "Onartutako gastua")
    {
        var moztuta = datuak.Take(KategoriaGehienezkoKopuruaGrafikoan).ToList();
        if (moztuta.Count == 0)
            return new KategoriaGrafikoEmaitza { ErakutsiHutsikMezua = true };

        if (moztuta.Count == 1)
        {
            var d = moztuta[0];
            var izena = string.IsNullOrWhiteSpace(d.KontzeptuIzena) ? "?" : d.KontzeptuIzena.Trim();
            return new KategoriaGrafikoEmaitza
            {
                ErakutsiBakarra = true,
                BakarXehetasuna =
                    $"{izena}: {d.Guztira.ToString("N2", KulturaZenbakietarako)} € (100%)"
            };
        }

        if (moztuta.Count >= KategoriaBarraGutxienezkoZatituak)
        {
            var barra = EraikiKategoriaBarraGrafikoa(moztuta, barraSerieIzena);
            return new KategoriaGrafikoEmaitza
            {
                ErakutsiBarraGrafikoa = true,
                BarraSerieak = barra.Serieak,
                BarraYArdatzak = barra.YArdatzak,
                BarraXArdatzak = barra.XArdatzak
            };
        }

        return new KategoriaGrafikoEmaitza
        {
            ErakutsiPieGrafikoa = true,
            GastuSerieak = EraikiKategoriaPieSerieak(moztuta)
        };
    }

    private static ISeries[] EraikiKategoriaPieSerieak(IReadOnlyList<KategoriakoGastuAgregatua> moztuta)
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

        return serieak.ToArray();
    }

    private static (ISeries[] Serieak, Axis[] YArdatzak, Axis[] XArdatzak) EraikiKategoriaBarraGrafikoa(
        IReadOnlyList<KategoriakoGastuAgregatua> moztuta,
        string barraSerieIzena)
    {
        var balioak = moztuta.Select(d => d.Guztira).ToArray();
        var etiketak = moztuta
            .Select(d => string.IsNullOrWhiteSpace(d.KontzeptuIzena) ? "?" : d.KontzeptuIzena.Trim())
            .ToArray();
        var guztira = balioak.Sum();

        var serieak = new ISeries[]
        {
            new RowSeries<double>
            {
                Name = barraSerieIzena,
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

        var yArdatzak = new Axis[]
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

        var xArdatzak = new Axis[]
        {
            new Axis
            {
                MinLimit = 0,
                Labeler = v => $"{v.ToString("N0", KulturaZenbakietarako)} €"
            }
        };

        return (serieak, yArdatzak, xArdatzak);
    }
}
