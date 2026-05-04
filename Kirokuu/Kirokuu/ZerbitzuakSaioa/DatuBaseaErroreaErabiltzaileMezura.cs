using System.Text.RegularExpressions;

namespace Kirokuu.ZerbitzuakSaioa;

/// <summary>
/// SQLite / libSQL errore-testuetatik taula eta zutabe izen seguruak ateratzen ditu (alfanumeriko + _ soilik).
/// </summary>
public static class DatuBaseaErroreaErabiltzaileMezura
{
    private static readonly Regex IzenSegurua = new("^[A-Za-z0-9_]+$", RegexOptions.Compiled);

    private static readonly Regex EzDagoTaula = new(
        @"no\s+such\s+table:\s*(?<t>[A-Za-z0-9_]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex EzDagoZutabea = new(
        @"no\s+such\s+column:\s*(?<c>[A-Za-z0-9_]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TaulakEzDuZutaberik = new(
        @"table\s+(?<t>[A-Za-z0-9_]+)\s+has\s+no\s+column\s+named\s+(?<c>[A-Za-z0-9_]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ZutabeaInBarne = new(
        @"no\s+such\s+column:\s*(?<c>[A-Za-z0-9_]+)\s+in",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static string? SegurtatuIzena(string? balioa) =>
        !string.IsNullOrEmpty(balioa) && IzenSegurua.IsMatch(balioa) ? balioa : null;

    /// <summary>
    /// Mezu nagusia (erabiltzailearentzat) eta xehetasun laburra (taula/zutabe), eskema-erroreak badira.
    /// </summary>
    public static (string? Nagusia, string? Xehetasuna) EskuratuEskemaMezuak(string? testua)
    {
        if (string.IsNullOrWhiteSpace(testua))
            return (null, null);

        var t = testua;

        var mTaulaZutabe = TaulakEzDuZutaberik.Match(t);
        if (mTaulaZutabe.Success)
        {
            var taula = SegurtatuIzena(mTaulaZutabe.Groups["t"].Value);
            var zutabea = SegurtatuIzena(mTaulaZutabe.Groups["c"].Value);
            var xehetasuna = OsatuXehetasunTaulaZutabe(taula, zutabea);
            return (
                "Zutabe edo eskema ez dator bat: datu-basearen egitura desberdina da aplikazioak espero duenarekiko.",
                xehetasuna);
        }

        var mTaula = EzDagoTaula.Match(t);
        if (mTaula.Success)
        {
            var taula = SegurtatuIzena(mTaula.Groups["t"].Value);
            return (
                "Taula ez da aurkitu edo izena okerra da.",
                taula is null ? null : $"Taula: '{taula}'.");
        }

        var mZutabe = EzDagoZutabea.Match(t);
        if (mZutabe.Success)
        {
            var zutabea = SegurtatuIzena(mZutabe.Groups["c"].Value);
            return (
                "Zutabea ez da existitzen edo izena okerra da.",
                zutabea is null ? null : $"Zutabea: '{zutabea}'.");
        }

        var mZutabeIn = ZutabeaInBarne.Match(t);
        if (mZutabeIn.Success)
        {
            var zutabea = SegurtatuIzena(mZutabeIn.Groups["c"].Value);
            return (
                "Zutabea ez da existitzen edo izena okerra da.",
                zutabea is null ? null : $"Zutabea: '{zutabea}'.");
        }

        return (null, null);
    }

    private static string? OsatuXehetasunTaulaZutabe(string? taula, string? zutabea)
    {
        if (taula is not null && zutabea is not null)
            return $"Taula: '{taula}'. Zutabea: '{zutabea}'.";
        if (taula is not null)
            return $"Taula: '{taula}'.";
        if (zutabea is not null)
            return $"Zutabea: '{zutabea}'.";
        return null;
    }

    /// <summary>
    /// SQLite salbuespenaren mezua mapatzen du eskema-erroreetarako.
    /// </summary>
    public static (string? Nagusia, string? Xehetasuna) EskuratuSqliteMezuak(string? sqliteMezua) =>
        EskuratuEskemaMezuak(sqliteMezua);
}
