using System.Globalization;

namespace Kirokuu.ZerbitzuakSaioa;

public static class ShellQueryLaguntzailea
{
    public static bool SaiatuParseatuId(string? queryBalioa, out int id)
    {
        id = 0;
        if (string.IsNullOrWhiteSpace(queryBalioa))
            return false;

        var garbia = Uri.UnescapeDataString(queryBalioa.Trim());
        return int.TryParse(garbia, NumberStyles.Integer, CultureInfo.InvariantCulture, out id) && id > 0;
    }
}
