using System.Collections.ObjectModel;
using Kirokuu.DatuEreduak;

namespace Kirokuu.Zerbitzuak;

public static class SektoreaKargoHautapenLaguntzailea
{
    public static void BeteKargoenZerrenda(
        ObservableCollection<HautapenElementua> kargoenAukerak,
        string? sektorea)
    {
        kargoenAukerak.Clear();
        if (string.IsNullOrWhiteSpace(sektorea))
            return;

        foreach (var k in SektoreaKargoarenHiztegia.SortuKargoenZerrenda(sektorea))
            kargoenAukerak.Add(k);
    }

    public static HautapenElementua? BilatuIdentifikatzaileaz(
        IEnumerable<HautapenElementua> zerrenda,
        int identifikatzailea)
    {
        if (identifikatzailea <= 0)
            return null;

        return zerrenda.FirstOrDefault(x => x.Identifikatzailea == identifikatzailea);
    }
}
