using System.Security.Cryptography;
using System.Text;

namespace Kirokuu.ZerbitzuakSaioa;

public sealed class PasahitzaZerbitzua
{
    private const int GatzaByteKopurua = 16;
    private const char GatzaHashBanaketa = '|';

    public (string GatzaBase64, string HashBase64) SortuGatzaEtaHash(string pasahitza)
    {
        ArgumentNullException.ThrowIfNull(pasahitza);
        var gatza = new byte[GatzaByteKopurua];
        RandomNumberGenerator.Fill(gatza);
        var gatzaBase64 = Convert.ToBase64String(gatza);
        var hashBase64 = SortuHash(pasahitza, gatzaBase64);
        return (gatzaBase64, hashBase64);
    }

    public string LotuGatzaEtaHashKatean(string gatzaBase64, string hashBase64)
    {
        ArgumentNullException.ThrowIfNull(gatzaBase64);
        ArgumentNullException.ThrowIfNull(hashBase64);
        if (gatzaBase64.Contains(GatzaHashBanaketa) ||
            hashBase64.Contains(GatzaHashBanaketa))
            throw new ArgumentException("Pasahitzaren barruko banaketa karakterea ez da onartzen.");

        return $"{gatzaBase64}{GatzaHashBanaketa}{hashBase64}";
    }

    public bool EgiaztatuGordetakoKatearekin(string pasahitza, string gordetakoKatea)
    {
        ArgumentNullException.ThrowIfNull(pasahitza);
        ArgumentNullException.ThrowIfNull(gordetakoKatea);
        var pasahitzaGarbia = pasahitza.Trim();
        var kateGarbia = gordetakoKatea.Trim();
        if (pasahitzaGarbia.Length == 0 || kateGarbia.Length == 0)
            return false;

        var zatiak = kateGarbia.Split(GatzaHashBanaketa, 2, StringSplitOptions.None);
        if (zatiak.Length != 2)
            return false;

        var gatzaGarbia = zatiak[0].Trim();
        var hashGarbia = zatiak[1].Trim();
        if (gatzaGarbia.Length == 0 || hashGarbia.Length == 0)
            return false;

        try
        {
            return Egiaztatu(pasahitzaGarbia, gatzaGarbia, hashGarbia);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public bool Egiaztatu(string pasahitza, string gatzaBase64, string hashBase64)
    {
        ArgumentNullException.ThrowIfNull(pasahitza);
        ArgumentNullException.ThrowIfNull(gatzaBase64);
        ArgumentNullException.ThrowIfNull(hashBase64);
        var pasahitzaGarbia = pasahitza.Trim();
        var gatzaGarbia = gatzaBase64.Trim();
        var hashGarbia = hashBase64.Trim();
        var esperoHash = Convert.FromBase64String(SortuHash(pasahitzaGarbia, gatzaGarbia));
        var gordetakoHash = Convert.FromBase64String(hashGarbia);
        return esperoHash.Length == gordetakoHash.Length &&
               CryptographicOperations.FixedTimeEquals(esperoHash, gordetakoHash);
    }

    private static string SortuHash(string pasahitza, string gatzaBase64)
    {
        var sarrera = Encoding.UTF8.GetBytes(pasahitza + gatzaBase64);
        var hash = SHA256.HashData(sarrera);
        return Convert.ToBase64String(hash);
    }
}
