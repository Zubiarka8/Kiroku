using System.Security.Cryptography;
using System.Text;

namespace Kirokuu.ZerbitzuakSaioa;

public sealed class PasahitzaZerbitzua
{
    private const int GatzaByteKopurua = 16;

    public (string GatzaBase64, string HashBase64) SortuGatzaEtaHash(string pasahitza)
    {
        ArgumentNullException.ThrowIfNull(pasahitza);
        var gatza = new byte[GatzaByteKopurua];
        RandomNumberGenerator.Fill(gatza);
        var gatzaBase64 = Convert.ToBase64String(gatza);
        var hashBase64 = SortuHash(pasahitza, gatzaBase64);
        return (gatzaBase64, hashBase64);
    }

    public bool Egiaztatu(string pasahitza, string gatzaBase64, string hashBase64)
    {
        ArgumentNullException.ThrowIfNull(pasahitza);
        ArgumentNullException.ThrowIfNull(gatzaBase64);
        ArgumentNullException.ThrowIfNull(hashBase64);
        var esperoHash = Convert.FromBase64String(SortuHash(pasahitza, gatzaBase64));
        var gordetakoHash = Convert.FromBase64String(hashBase64);
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
