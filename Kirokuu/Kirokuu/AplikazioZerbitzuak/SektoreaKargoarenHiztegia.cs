using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.DatuEreduak;
using Kirokuu.Zerbitzuak;

namespace Kirokuu.AplikazioZerbitzuak;

public static class SektoreaKargoarenHiztegia
{
    private static readonly HautapenElementua[] SektoreenZerrenda =
    {
        new HautapenElementua { Identifikatzailea = (int)EnpresakoSektorea.Finantzak, Etiketa = SektoreaBalioak.Finantzak },
        new HautapenElementua { Identifikatzailea = (int)EnpresakoSektorea.Marketina, Etiketa = SektoreaBalioak.Marketina },
        new HautapenElementua { Identifikatzailea = (int)EnpresakoSektorea.Salmentak, Etiketa = SektoreaBalioak.Salmentak }
    };

    public static IReadOnlyList<HautapenElementua> SortuSektoreenZerrenda() => SektoreenZerrenda;

    public static string LortuSektorearenEtiketa(int sektorearenIdentifikatzailea) =>
        SektoreaBalioak.LortuSektorearenEtiketa(sektorearenIdentifikatzailea);

    /// <summary>
    /// BidaiaTxostena.Saila gordetzeko testu baliozkoa: Finantzak, Marketina edo Salmentak.
    /// </summary>
    public static string LortuBaliozkotutakoSailaTestua(string? sektoreaEdoSailaTestua)
    {
        if (string.IsNullOrWhiteSpace(sektoreaEdoSailaTestua))
            return string.Empty;

        var garbia = sektoreaEdoSailaTestua.Trim();
        foreach (var s in SektoreenZerrenda)
        {
            if (string.Equals(s.Etiketa, garbia, StringComparison.Ordinal))
                return s.Etiketa;
        }

        return string.Empty;
    }

    public static IReadOnlyList<HautapenElementua> SortuKargoenZerrenda(int sektorearenIdentifikatzailea)
    {
        return (EnpresakoSektorea)sektorearenIdentifikatzailea switch
        {
            EnpresakoSektorea.Finantzak =>
            [
                new HautapenElementua { Identifikatzailea = (int)EnpresakoLangileKargoa.Kontularia, Etiketa = "Kontularia" },
                new HautapenElementua { Identifikatzailea = (int)EnpresakoLangileKargoa.FinantzaAnalista, Etiketa = "Finantza analista" },
                new HautapenElementua { Identifikatzailea = (int)EnpresakoLangileKargoa.Auditorra, Etiketa = "Auditatzailea" },
                new HautapenElementua { Identifikatzailea = (int)EnpresakoLangileKargoa.AholkulariFiskala, Etiketa = "Zerga aholkularia" }
            ],
            EnpresakoSektorea.Marketina =>
            [
                new HautapenElementua { Identifikatzailea = (int)EnpresakoLangileKargoa.KomunitateKudeatzailea, Etiketa = "Komunitate kudeatzailea" },
                new HautapenElementua { Identifikatzailea = (int)EnpresakoLangileKargoa.DiseinatzaileGrafikoa, Etiketa = "Diseinu grafikoa" },
                new HautapenElementua { Identifikatzailea = (int)EnpresakoLangileKargoa.SeoEspezialista, Etiketa = "SEO espezialista" },
                new HautapenElementua { Identifikatzailea = (int)EnpresakoLangileKargoa.Copywriter, Etiketa = "Idazle sortzailea" }
            ],
            EnpresakoSektorea.Salmentak =>
            [
                new HautapenElementua { Identifikatzailea = (int)EnpresakoLangileKargoa.Komertziala, Etiketa = "Komertziala" },
                new HautapenElementua { Identifikatzailea = (int)EnpresakoLangileKargoa.AccountManager, Etiketa = "Kontu kudeatzailea" },
                new HautapenElementua { Identifikatzailea = (int)EnpresakoLangileKargoa.SalmentenArduraduna, Etiketa = "Salmenta kudeatzailea" },
                new HautapenElementua { Identifikatzailea = (int)EnpresakoLangileKargoa.BezeroArreta, Etiketa = "Bezero arreta" }
            ],
            _ => []
        };
    }

    public static EnpresakoSektorea? LortuSektoreaKargoarentzat(EnpresakoLangileKargoa kargo)
    {
        return kargo switch
        {
            EnpresakoLangileKargoa.Kontularia or EnpresakoLangileKargoa.FinantzaAnalista or EnpresakoLangileKargoa.Auditorra
                or EnpresakoLangileKargoa.AholkulariFiskala => EnpresakoSektorea.Finantzak,
            EnpresakoLangileKargoa.KomunitateKudeatzailea or EnpresakoLangileKargoa.DiseinatzaileGrafikoa
                or EnpresakoLangileKargoa.SeoEspezialista or EnpresakoLangileKargoa.Copywriter => EnpresakoSektorea.Marketina,
            EnpresakoLangileKargoa.Komertziala or EnpresakoLangileKargoa.AccountManager or EnpresakoLangileKargoa.SalmentenArduraduna
                or EnpresakoLangileKargoa.BezeroArreta => EnpresakoSektorea.Salmentak,
            EnpresakoLangileKargoa.AdministratzaileSistema => EnpresakoSektorea.Finantzak,
            _ => null
        };
    }

    public static string LortuKargoarenEtiketa(EnpresakoLangileKargoa kargo)
    {
        if (kargo == EnpresakoLangileKargoa.AdministratzaileSistema)
            return "Sistema administratzailea";

        foreach (var sektorea in new[] { EnpresakoSektorea.Finantzak, EnpresakoSektorea.Marketina, EnpresakoSektorea.Salmentak })
        {
            foreach (var el in SortuKargoenZerrenda((int)sektorea))
            {
                if (el.Identifikatzailea == (int)kargo)
                    return el.Etiketa;
            }
        }

        return string.Empty;
    }

    public static bool SektoreaEtaKargoarenIdentifikatzaileakBaliozkoa(int sektorearenIdentifikatzailea, int kargoarenIdentifikatzailea) =>
        sektorearenIdentifikatzailea > 0 && kargoarenIdentifikatzailea > 0 &&
        KargoakSektorearekinBatDator(sektorearenIdentifikatzailea, kargoarenIdentifikatzailea);

    public static bool KargoakSektorearekinBatDator(int sektorearenIdentifikatzailea, int kargoarenIdentifikatzailea)
    {
        if (sektorearenIdentifikatzailea <= 0 || kargoarenIdentifikatzailea <= 0)
            return false;

        var k = (EnpresakoLangileKargoa)kargoarenIdentifikatzailea;
        var esperoDenSektorea = LortuSektoreaKargoarentzat(k);
        return esperoDenSektorea == (EnpresakoSektorea)sektorearenIdentifikatzailea;
    }

    public static bool SaiatuLeheneratuTestutik(string? kargoarenTestuaZaharra, ref int sektorearenIdentifikatzailea, ref int kargoarenIdentifikatzailea)
    {
        if (string.IsNullOrWhiteSpace(kargoarenTestuaZaharra) || kargoarenIdentifikatzailea > 0)
            return false;

        var garbia = kargoarenTestuaZaharra.Trim();
        foreach (var sektorea in new[] { EnpresakoSektorea.Finantzak, EnpresakoSektorea.Marketina, EnpresakoSektorea.Salmentak })
        {
            foreach (var el in SortuKargoenZerrenda((int)sektorea))
            {
                if (string.Equals(el.Etiketa, garbia, StringComparison.OrdinalIgnoreCase))
                {
                    kargoarenIdentifikatzailea = el.Identifikatzailea;
                    sektorearenIdentifikatzailea = (int)(LortuSektoreaKargoarentzat((EnpresakoLangileKargoa)el.Identifikatzailea)
                        ?? EnpresakoSektorea.EzDaZehaztu);
                    return true;
                }
            }
        }

        return false;
    }
}
