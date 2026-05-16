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
            return DataOrduaOsatu(DateTime.UtcNow);

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

        return DataOrduaOsatu(DateTime.UtcNow);
    }

    public static string DataOrduaOsatuHautatutakoEguna(DateTime hautatutakoData)
    {
        var orainUtc = DateTime.UtcNow;
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
}
