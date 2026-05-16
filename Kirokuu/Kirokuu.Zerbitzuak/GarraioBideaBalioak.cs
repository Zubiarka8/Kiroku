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
}
