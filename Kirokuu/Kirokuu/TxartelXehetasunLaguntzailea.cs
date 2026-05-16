using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.Zerbitzuak;

namespace Kirokuu;

public static class TxartelXehetasunLaguntzailea
{
    public static bool IbilgailuaErabiltzenDu(IReadOnlyList<GastuLerroa> lerroak) =>
        lerroak.Any(l => GarraioBideaBalioak.IbilgailuaErabiltzenDu(l.GarraioBidea));

    public static bool KilometroakIkagarri(IReadOnlyList<GastuLerroa> lerroak) =>
        lerroak.Any(l => l.Kilometroak > 0);

    public static double KilometroakGuztira(IReadOnlyList<GastuLerroa> lerroak) =>
        lerroak.Sum(l => l.Kilometroak);
}
