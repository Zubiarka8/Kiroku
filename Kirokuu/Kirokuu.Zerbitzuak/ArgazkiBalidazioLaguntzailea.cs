namespace Kirokuu.Zerbitzuak;

public static class ArgazkiBalidazioLaguntzailea
{
    private const long GehienezkoTamainaByte = 10 * 1024 * 1024;

    public static string? EgiaztatuFitxategia(string? fitxategiBidea)
    {
        if (string.IsNullOrWhiteSpace(fitxategiBidea))
            return "Ez da argazkirik hautatu.";

        if (!File.Exists(fitxategiBidea))
            return "Ez da aurkitu hautatutako fitxategia.";

        var luzapena = Path.GetExtension(fitxategiBidea);
        if (!string.Equals(luzapena, ".jpg", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(luzapena, ".jpeg", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(luzapena, ".png", StringComparison.OrdinalIgnoreCase))
        {
            return "Formatuak JPEG edo PNG soilik onartzen dira.";
        }

        var info = new FileInfo(fitxategiBidea);
        if (info.Length > GehienezkoTamainaByte)
            return "Argazkiak gehienez 10 MB izan dezake.";

        return null;
    }
}
