namespace Kirokuu.Zerbitzuak;

public static class GarraioBideaBalioak
{
    public const string EnpresakoIbilgailua = "Enpresako ibilgailua";

    public const string GarraioPublikoa = "Garraio publikoa";

    public static IReadOnlyList<string> AukeraEstadioak { get; } = new[]
    {
        EnpresakoIbilgailua,
        GarraioPublikoa
    };
}
