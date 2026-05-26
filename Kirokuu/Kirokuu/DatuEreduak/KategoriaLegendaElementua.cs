using Microsoft.Maui.Graphics;

namespace Kirokuu.DatuEreduak;

/// <summary>
/// Kategoria pie grafikoaren legenda propioko elementu bat (kolorea + etiketa irakurgarriak).
/// </summary>
public sealed class KategoriaLegendaElementua
{
    public Color Kolorea { get; set; } = Colors.Gray;

    public string Izena { get; set; } = string.Empty;

    public string Xehetasuna { get; set; } = string.Empty;
}
