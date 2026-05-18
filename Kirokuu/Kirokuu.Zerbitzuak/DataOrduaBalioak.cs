using System.Globalization;

namespace Kirokuu.Zerbitzuak;

/// <summary>
/// Data-ordu testuak ISO 8601 UTC osatu gisa (eguna, ordua, minutuak eta segundoak) normalizatzeko.
/// </summary>
public static class DataOrduaBalioak
{
    private const string IdazketaFormatua = "o";

    private const string BistaratzeFormatua = "dd/MM/yyyy HH:mm:ss";

    private const string DataSoilaFormatua = "yyyy-MM-dd";

    /// <summary>
    /// Uneko ordua UTC. Erabili DateTime motako zutabeetan (AuditoretzaLoga.DataOrdua, ...).
    /// </summary>
    public static DateTime OrduaUtcOrain() => DateTime.UtcNow;

    /// <summary>
    /// Uneko data-ordua ISO 8601 testu gisa. Erabili TEXT zutabeetan (HasieraData, SorkuntzaData, ...).
    /// </summary>
    public static string DataOrduaOrain() => DataOrduaOsatu(OrduaUtcOrain());

    /// <summary>
    /// Mapa/Turso erantzunetik zutabe bat irakurri eta idazketa-formatura normalizatu.
    /// Taula independentea: gakoa soilik behar da (HasieraData, GastuData, DataOrdua, ...).
    /// </summary>
    public static string MapatikDataOrdua(IReadOnlyDictionary<string, string>? mapa, string zutabeIzena)
    {
        if (mapa is null || string.IsNullOrWhiteSpace(zutabeIzena))
            return DataOrduaOrain();

        return TryGetMapaBalioa(mapa, zutabeIzena, out var testua)
            ? DataOrduaOsatu(testua)
            : DataOrduaOrain();
    }

    /// <summary>
    /// Mapa/Turso erantzunetik zutabe bat DateTime UTC gisa irakurri (zutabe hutsa → orain).
    /// </summary>
    public static DateTime MapatikOrduaUtc(IReadOnlyDictionary<string, string>? mapa, string zutabeIzena)
    {
        if (mapa is null || string.IsNullOrWhiteSpace(zutabeIzena))
            return OrduaUtcOrain();

        if (!TryGetMapaBalioa(mapa, zutabeIzena, out var testua) || string.IsNullOrWhiteSpace(testua))
            return OrduaUtcOrain();

        if (DateTime.TryParse(
                testua.Trim(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces,
                out var dataOrdua))
        {
            return dataOrdua.Kind switch
            {
                DateTimeKind.Utc => dataOrdua,
                DateTimeKind.Local => dataOrdua.ToUniversalTime(),
                _ => DateTime.SpecifyKind(dataOrdua, DateTimeKind.Utc)
            };
        }

        return OrduaUtcOrain();
    }

    public static string DataOrduaOsatu(DateTime data)
    {
        var utc = data.Kind switch
        {
            DateTimeKind.Utc => data,
            DateTimeKind.Local => data.ToUniversalTime(),
            _ => DateTime.SpecifyKind(data, DateTimeKind.Local).ToUniversalTime()
        };
        return utc.ToString(IdazketaFormatua, CultureInfo.InvariantCulture);
    }

    public static string DataOrduaOsatu(string? gordetakoTestua)
    {
        if (string.IsNullOrWhiteSpace(gordetakoTestua))
            return DataOrduaOrain();

        var testua = gordetakoTestua.Trim();

        if (testua.Length == 10
            && DateTime.TryParseExact(testua, DataSoilaFormatua, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dataSoila))
        {
            return DataOrduaOsatu(DateTime.SpecifyKind(dataSoila, DateTimeKind.Utc));
        }

        if (DateTime.TryParse(testua, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces, out var parsed))
        {
            return DataOrduaOsatu(parsed);
        }

        return DataOrduaOrain();
    }

    public static string DataOrduaOsatuHautatutakoEguna(DateTime hautatutakoData)
    {
        var orainUtc = OrduaUtcOrain();
        var eguna = hautatutakoData.Date;
        var dataOrdua = new DateTime(
            eguna.Year,
            eguna.Month,
            eguna.Day,
            orainUtc.Hour,
            orainUtc.Minute,
            orainUtc.Second,
            orainUtc.Millisecond,
            DateTimeKind.Utc);
        return DataOrduaOsatu(dataOrdua);
    }

    public static string DataOrduaBistaratu(string? gordetakoTestua)
    {
        if (string.IsNullOrWhiteSpace(gordetakoTestua))
            return string.Empty;

        if (DateTime.TryParse(gordetakoTestua.Trim(), CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces, out var data))
        {
            var lokala = data.Kind == DateTimeKind.Utc ? data.ToLocalTime() : data;
            return lokala.ToString(BistaratzeFormatua, CultureInfo.InvariantCulture);
        }

        return gordetakoTestua;
    }

    private static bool TryGetMapaBalioa(
        IReadOnlyDictionary<string, string> mapa,
        string gakoa,
        out string? testua)
    {
        if (mapa.TryGetValue(gakoa, out testua))
            return true;

        foreach (var pare in mapa)
        {
            if (!string.Equals(pare.Key, gakoa, StringComparison.OrdinalIgnoreCase))
                continue;

            testua = pare.Value;
            return true;
        }

        testua = null;
        return false;
    }
}
