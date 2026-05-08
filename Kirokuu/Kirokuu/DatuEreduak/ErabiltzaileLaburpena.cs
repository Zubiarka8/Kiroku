namespace Kirokuu.DatuEreduak;

public sealed class ErabiltzaileLaburpena
{
    public int Id { get; set; }

    public string Izena { get; set; } = string.Empty;

    public string Abizena { get; set; } = string.Empty;

    public string Posta { get; set; } = string.Empty;

    public int Aktiboa { get; set; } = 1;

    public string AktiboTestua => Aktiboa != 0 ? "Aktibo" : "Desaktibatuta";
}
