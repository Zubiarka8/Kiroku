namespace Kirokuu.Zerbitzuak;

public static class GarraioBideaBalioak
{
    public const string EnpresakoIbilgailua = "Enpresako ibilgailua";

    public const string NorberarenIbilgailua = "Norberaren ibilgailua";

    public const string GarraioPublikoa = "Garraio publikoa";

    public static IReadOnlyList<string> AukeraEstadioak { get; } = new[]
    {
        EnpresakoIbilgailua,
        NorberarenIbilgailua,
        GarraioPublikoa
    };

    public static bool IbilgailuaErabiltzenDu(string garraioBidea) =>
        garraioBidea == EnpresakoIbilgailua || garraioBidea == NorberarenIbilgailua;

    public static bool DaEnpresakoIbilgailua(string? garraioBidea) =>
        string.Equals(garraioBidea?.Trim(), EnpresakoIbilgailua, StringComparison.Ordinal);

    public static int EnpresakoIbilgailuaBandera(string? garraioBidea) =>
        DaEnpresakoIbilgailua(garraioBidea) ? 1 : 0;

    public static string SortuGarraioBideaTestua(bool enpresakoIbilgailua) =>
        enpresakoIbilgailua ? EnpresakoIbilgailua : NorberarenIbilgailua;

    public static string IbilgailuaMotaEtiketa(string? garraioBidea, int empresaIbilgailuaBandera)
    {
        if (DaEnpresakoIbilgailua(garraioBidea) || empresaIbilgailuaBandera == 1)
            return EnpresakoIbilgailua;
        if (string.Equals(garraioBidea?.Trim(), NorberarenIbilgailua, StringComparison.Ordinal))
            return NorberarenIbilgailua;
        if (string.Equals(garraioBidea?.Trim(), GarraioPublikoa, StringComparison.Ordinal))
            return GarraioPublikoa;
        return string.Empty;
    }
}
