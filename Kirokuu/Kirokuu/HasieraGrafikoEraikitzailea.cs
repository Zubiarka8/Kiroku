using System.Globalization;
using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.DatuEreduak;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace Kirokuu;

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
}
