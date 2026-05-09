namespace Kirokuu.DatuEreduak;

public sealed class HautapenElementua : IEquatable<HautapenElementua>
{
    public int Identifikatzailea { get; init; }

    public string Etiketa { get; init; } = string.Empty;

    public bool Equals(HautapenElementua? bestea) =>
        bestea is not null && Identifikatzailea == bestea.Identifikatzailea;

    public override bool Equals(object? obj) => obj is HautapenElementua h && Equals(h);

    public override int GetHashCode() => Identifikatzailea;
}
