namespace Kirokuu.Zerbitzuak;

public enum KredentzialEgiaztapenMota
{
    Ongi,
    Gaizki,
    Errorea
}

/// <summary>
/// Outcome of the combined Turso + Cloudinary credential probe.
/// </summary>
public sealed record KredentzialEgiaztapenEmaitza(KredentzialEgiaztapenMota Mota, string? Xehetasuna);
