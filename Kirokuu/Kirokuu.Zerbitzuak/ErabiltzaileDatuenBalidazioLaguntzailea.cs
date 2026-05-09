using System.Globalization;
using System.Text.RegularExpressions;

namespace Kirokuu.Zerbitzuak;

/// <summary>
/// NAN / IFZ (DNI eta NIE espainiarrak), posta eta izenen balidazio laguntzaileak.
/// </summary>
public static class ErabiltzaileDatuenBalidazioLaguntzailea
{
    private const string KontrolLetrenKatea = "TRWAGMYFPDXBNJZSQVHLCKE";

    private static readonly Regex PostaEredua = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>
    /// Baliozkotzen du NAN edo IFZ testua (DNI: 8 zenbaki + letra; NIE: X/Y/Z + 7 zenbaki + letra).
    /// Tartea eta gidoiak baztertzen ditu.
    /// </summary>
    public static bool NanEdoIfzBaliozkoa(string? testua)
    {
        if (string.IsNullOrWhiteSpace(testua))
            return false;

        var garbia = string.Concat(testua.Where(static c => !char.IsWhiteSpace(c) && c != '-'));
        if (garbia.Length != 9)
            return false;

        garbia = garbia.ToUpperInvariant();

        string zortziZenbakiKatea;
        if (char.IsDigit(garbia[0]))
        {
            for (var i = 0; i < 8; i++)
            {
                if (!char.IsDigit(garbia[i]))
                    return false;
            }

            if (!char.IsLetter(garbia[8]))
                return false;

            zortziZenbakiKatea = garbia[..8];
        }
        else if (garbia[0] is 'X' or 'Y' or 'Z')
        {
            for (var i = 1; i < 8; i++)
            {
                if (!char.IsDigit(garbia[i]))
                    return false;
            }

            if (!char.IsLetter(garbia[8]))
                return false;

            var lehena = garbia[0] switch
            {
                'X' => '0',
                'Y' => '1',
                'Z' => '2',
                _ => '\0'
            };
            zortziZenbakiKatea = string.Concat(lehena, garbia.Substring(1, 7));
        }
        else
        {
            return false;
        }

        if (!ulong.TryParse(zortziZenbakiKatea, NumberStyles.None, CultureInfo.InvariantCulture, out var zenbakia))
            return false;

        var esperoDenLetra = KontrolLetrenKatea[(int)(zenbakia % 23)];
        return garbia[8] == esperoDenLetra;
    }

    /// <summary>
    /// Posta helbidearen formatua egiaztatzen du (luzeraren eta egituraren arabera).
    /// </summary>
    public static bool PostaBaliozkoa(string? posta)
    {
        if (string.IsNullOrWhiteSpace(posta))
            return false;

        var trimatua = posta.Trim();
        if (trimatua.Length > 254)
            return false;

        return PostaEredua.IsMatch(trimatua);
    }

    /// <summary>
    /// Izen edo abizen laburrarentzat: gutxienez letra bat, karaktere onartuak.
    /// </summary>
    public static bool PertsonaIzenLaburraBaliozkoa(string? testua, int gutxienezKaraktere, int gehienekoKaraktere)
    {
        if (string.IsNullOrWhiteSpace(testua))
            return false;

        var t = testua.Trim();
        if (t.Length < gutxienezKaraktere || t.Length > gehienekoKaraktere)
            return false;

        if (!t.Any(static ch => char.IsLetter(ch)))
            return false;

        foreach (var ch in t)
        {
            if (char.IsLetter(ch) || char.IsWhiteSpace(ch) || ch is '-' or '\'' or '\u2019')
                continue;

            return false;
        }

        return true;
    }

    /// <summary>
    /// Kargoaren testurako balidazio orokorra.
    /// </summary>
    public static bool KargoaTestuaBaliozkoa(string? testua, int gutxienezKaraktere, int gehienekoKaraktere)
    {
        if (string.IsNullOrWhiteSpace(testua))
            return false;

        var t = testua.Trim();
        return t.Length >= gutxienezKaraktere && t.Length <= gehienekoKaraktere;
    }
}
