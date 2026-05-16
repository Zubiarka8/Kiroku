using System.Reflection;

namespace Kirokuu.DatuBasea;

public static class DatuBaseaSortzeAginduak
{
    public static string IrakurriSortzeAgindua(string fitxategiIzena)
    {
        var assembly = typeof(DatuBaseaSortzeAginduak).Assembly;
        var baliabideIzena = $"Kirokuu.DatuBasea.SortzeAginduak.{fitxategiIzena}";
        using var jarioa = assembly.GetManifestResourceStream(baliabideIzena);
        if (jarioa is null)
            throw new InvalidOperationException($"Ez da aurkitu sortze agindua: {fitxategiIzena}");

        using var irakurgailua = new StreamReader(jarioa);
        return irakurgailua.ReadToEnd();
    }
}
