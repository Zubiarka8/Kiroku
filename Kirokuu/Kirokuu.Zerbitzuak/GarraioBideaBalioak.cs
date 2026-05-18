namespace Kirokuu.Zerbitzuak;

public static class GarraioBideaBalioak
{
    public const string EnpresakoIbilgailua = "Enpresako ibilgailua";

    public const string NorberarenIbilgailua = "Norberaren ibilgailua";

    public const string GarraioPublikoa = "Garraio publikoa";

    public static IList<string> AukeraEstadioak { get; } = new List<string>
    {
        EnpresakoIbilgailua,
        NorberarenIbilgailua,
        GarraioPublikoa
    };

    public static bool IbilgailuaErabiltzenDu(string garraioBidea) =>
        garraioBidea == EnpresakoIbilgailua || garraioBidea == NorberarenIbilgailua;
}
