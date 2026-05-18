using System.Security.Cryptography;

namespace Kirokuu.ZerbitzuakSaioa;

// SQLCipher gakoaren laguntzailea: SecureStorage-en 32 byte (hex).
internal static class DatuBaseaZifraketaLaguntzailea
{
    internal const string GakoarenBiltegiGakoa = "kiroku_datubase_sqlcipher_gako_hex";

    internal static byte[] SortuZufakoGakoByteak()
    {
        var buf = new byte[32];
        RandomNumberGenerator.Fill(buf);
        return buf;
    }
}
