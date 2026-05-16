using System.Globalization;

namespace Kirokuu.Zerbitzuak;

public static class ZenbatekoaBalidazioLaguntzailea
{
    public static bool SaiatuParseatuDezimala(string? testua, out double zenbatekoa)
    {
        zenbatekoa = 0;
        if (string.IsNullOrWhiteSpace(testua))
            return false;

        var normalizatua = testua.Trim().Replace(',', '.');
        return double.TryParse(
            normalizatua,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out zenbatekoa);
    }
}
