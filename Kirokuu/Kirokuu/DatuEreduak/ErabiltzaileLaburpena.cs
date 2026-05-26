using Kirokuu.DatuBasea.Ereduak;

namespace Kirokuu.DatuEreduak;

public sealed class ErabiltzaileLaburpena
{
    public int Id { get; set; }

    public string Izena { get; set; } = string.Empty;

    public string Abizena { get; set; } = string.Empty;

    public string Posta { get; set; } = string.Empty;

    public string Sektorea { get; set; } = string.Empty;

    public int Aktiboa { get; set; } = 1;

    public int Rola { get; set; } = (int)ErabiltzaileRola.Langilea;

    public string AktiboTestua => Aktiboa != 0 ? "Aktibo" : "Desaktibatuta";

    // Rol etiketak euskaraz.
    public string RolaTestua => Rola switch
    {
        (int)ErabiltzaileRola.Administratzailea => "Administratzailea",
        (int)ErabiltzaileRola.ZuzendariNagusia => "Zuzendari Nagusia (CEO)",
        _ => "Langilea"
    };

    public bool DaLangilea => Rola == (int)ErabiltzaileRola.Langilea;
}
