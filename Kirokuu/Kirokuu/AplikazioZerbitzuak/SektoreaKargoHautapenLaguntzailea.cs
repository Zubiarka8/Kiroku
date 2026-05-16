using System.Collections.ObjectModel;
using System.Linq;
using Kirokuu.DatuEreduak;

namespace Kirokuu.AplikazioZerbitzuak;

public static class SektoreaKargoHautapenLaguntzailea
{
    public static void BeteKargoenZerrenda(
        ObservableCollection<HautapenElementua> kargoenAukerak,
        HautapenElementua? hautatutakoSektorea)
    {
        kargoenAukerak.Clear();
        if (hautatutakoSektorea is null)
            return;

        foreach (var k in SektoreaKargoarenHiztegia.SortuKargoenZerrenda(hautatutakoSektorea.Identifikatzailea))
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
