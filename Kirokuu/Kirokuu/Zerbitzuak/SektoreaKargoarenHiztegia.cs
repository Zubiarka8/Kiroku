using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.DatuEreduak;

namespace Kirokuu.Zerbitzuak;

public static class SektoreaKargoarenHiztegia
{
    private static readonly string[] SektoreenZerrenda =
    {
        SektoreIzenak.Finantzak,
        SektoreIzenak.Marketina,
        SektoreIzenak.Salmentak
    };

    public static IReadOnlyList<string> SortuSektoreenZerrenda() => SektoreenZerrenda;

    public static IReadOnlyList<HautapenElementua> SortuKargoenZerrenda(string? sektorea)
    {
        if (string.IsNullOrWhiteSpace(sektorea))
            return [];

        return sektorea switch
        {
            SektoreIzenak.Finantzak =>
            [
                new HautapenElementua { Identifikatzailea = (int)EnpresakoLangileKargoa.Kontularia, Etiketa = "Kontularia" },
                new HautapenElementua { Identifikatzailea = (int)EnpresakoLangileKargoa.FinantzaAnalista, Etiketa = "Finantza analista" },
                new HautapenElementua { Identifikatzailea = (int)EnpresakoLangileKargoa.Auditorra, Etiketa = "Auditatzailea" },
                new HautapenElementua { Identifikatzailea = (int)EnpresakoLangileKargoa.AholkulariFiskala, Etiketa = "Zerga aholkularia" }
            ],
            SektoreIzenak.Marketina =>
            [
                new HautapenElementua { Identifikatzailea = (int)EnpresakoLangileKargoa.KomunitateKudeatzailea, Etiketa = "Komunitate kudeatzailea" },
                new HautapenElementua { Identifikatzailea = (int)EnpresakoLangileKargoa.DiseinatzaileGrafikoa, Etiketa = "Diseinu grafikoa" },
                new HautapenElementua { Identifikatzailea = (int)EnpresakoLangileKargoa.SeoEspezialista, Etiketa = "SEO espezialista" },
                new HautapenElementua { Identifikatzailea = (int)EnpresakoLangileKargoa.Copywriter, Etiketa = "Idazle sortzailea" }
            ],
            SektoreIzenak.Salmentak =>
            [
                new HautapenElementua { Identifikatzailea = (int)EnpresakoLangileKargoa.Komertziala, Etiketa = "Komertziala" },
                new HautapenElementua { Identifikatzailea = (int)EnpresakoLangileKargoa.AccountManager, Etiketa = "Kontu kudeatzailea" },
                new HautapenElementua { Identifikatzailea = (int)EnpresakoLangileKargoa.SalmentenArduraduna, Etiketa = "Salmenta kudeatzailea" },
                new HautapenElementua { Identifikatzailea = (int)EnpresakoLangileKargoa.BezeroArreta, Etiketa = "Bezero arreta" }
            ],
            _ => []
        };
    }

    public static string? LortuSektoreaKargoarentzat(EnpresakoLangileKargoa kargo)
    {
        return kargo switch
        {
            EnpresakoLangileKargoa.Kontularia or EnpresakoLangileKargoa.FinantzaAnalista or EnpresakoLangileKargoa.Auditorra
                or EnpresakoLangileKargoa.AholkulariFiskala => SektoreIzenak.Finantzak,
            EnpresakoLangileKargoa.KomunitateKudeatzailea or EnpresakoLangileKargoa.DiseinatzaileGrafikoa
                or EnpresakoLangileKargoa.SeoEspezialista or EnpresakoLangileKargoa.Copywriter => SektoreIzenak.Marketina,
            EnpresakoLangileKargoa.Komertziala or EnpresakoLangileKargoa.AccountManager or EnpresakoLangileKargoa.SalmentenArduraduna
                or EnpresakoLangileKargoa.BezeroArreta => SektoreIzenak.Salmentak,
            EnpresakoLangileKargoa.AdministratzaileSistema => SektoreIzenak.Finantzak,
            _ => null
        };
    }

    public static string LortuKargoarenEtiketa(EnpresakoLangileKargoa kargo)
    {
        if (kargo == EnpresakoLangileKargoa.AdministratzaileSistema)
            return "Sistema administratzailea";

        foreach (var sektorea in SektoreenZerrenda)
        {
            foreach (var el in SortuKargoenZerrenda(sektorea))
            {
                if (el.Identifikatzailea == (int)kargo)
                    return el.Etiketa;
            }
        }

        return string.Empty;
    }

    public static bool SektoreaEtaKargoarenIdentifikatzaileakBaliozkoa(string? sektorea, int kargoarenIdentifikatzailea) =>
        !string.IsNullOrWhiteSpace(sektorea) && kargoarenIdentifikatzailea > 0 &&
        KargoakSektorearekinBatDator(sektorea, kargoarenIdentifikatzailea);

    public static bool KargoakSektorearekinBatDator(string? sektorea, int kargoarenIdentifikatzailea)
    {
        if (string.IsNullOrWhiteSpace(sektorea) || kargoarenIdentifikatzailea <= 0)
            return false;

        var k = (EnpresakoLangileKargoa)kargoarenIdentifikatzailea;
        var esperoDenSektorea = LortuSektoreaKargoarentzat(k);
        return string.Equals(esperoDenSektorea, sektorea, StringComparison.Ordinal);
    }

    public static bool SaiatuLeheneratuTestutik(string? kargoarenTestuaZaharra, ref string? sektorea, ref int kargoarenIdentifikatzailea)
    {
        if (string.IsNullOrWhiteSpace(kargoarenTestuaZaharra) || kargoarenIdentifikatzailea > 0)
            return false;

        var garbia = kargoarenTestuaZaharra.Trim();
        foreach (var sek in SektoreenZerrenda)
        {
            foreach (var el in SortuKargoenZerrenda(sek))
            {
                if (string.Equals(el.Etiketa, garbia, StringComparison.OrdinalIgnoreCase))
                {
                    kargoarenIdentifikatzailea = el.Identifikatzailea;
                    sektorea = LortuSektoreaKargoarentzat((EnpresakoLangileKargoa)el.Identifikatzailea);
                    return true;
                }
            }
        }

        return false;
    }
}
