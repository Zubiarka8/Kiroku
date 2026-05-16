namespace Kirokuu.Zerbitzuak;

/// <summary>
/// Erabiltzaileak.Sektorea TEXT zutabea: Finantzak, Marketina, Salmentak.
/// </summary>
public static class SektoreaBalioak
{
    public const string Finantzak = "Finantzak";

    public const string Marketina = "Marketina";

    public const string Salmentak = "Salmentak";

    public static string LortuSektorearenEtiketa(int sektorearenIdentifikatzailea) =>
        sektorearenIdentifikatzailea switch
        {
            1 => Finantzak,
            2 => Marketina,
            3 => Salmentak,
            _ => string.Empty
        };
}
