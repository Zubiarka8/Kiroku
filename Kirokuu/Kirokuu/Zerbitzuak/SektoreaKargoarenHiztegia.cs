using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.DatuEreduak;

namespace Kirokuu.Zerbitzuak;

public static class SektoreaKargoarenHiztegia
{
    private static readonly HautapenElementua[] SektoreenZerrenda =
    {
        new HautapenElementua { Identifikatzailea = (int)EnpresakoSektorea.Finantzak, Etiketa = SektoreIzenak.Finantzak },
        new HautapenElementua { Identifikatzailea = (int)EnpresakoSektorea.Marketina, Etiketa = SektoreIzenak.Marketina },
        new HautapenElementua { Identifikatzailea = (int)EnpresakoSektorea.Salmentak, Etiketa = SektoreIzenak.Salmentak }
    };

    public static IReadOnlyList<HautapenElementua> SortuSektoreenZerrenda() => SektoreenZerrenda;

    public static string LortuSektorearenEtiketa(int sektorearenIdentifikatzailea)
    {
        foreach (var s in SektoreenZerrenda)
        {
            if (s.Identifikatzailea == sektorearenIdentifikatzailea)
                return s.Etiketa;
        }

        return string.Empty;
    }

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

    public static IReadOnlyList<HautapenElementua> SortuKargoenZerrenda(string? sektorea)
    {
        if (string.IsNullOrWhiteSpace(sektorea))
            return [];

        return sektorea switch
        {
            SektoreIzenak.Finantzak => SortuKargoenZerrenda((int)EnpresakoSektorea.Finantzak),
            SektoreIzenak.Marketina => SortuKargoenZerrenda((int)EnpresakoSektorea.Marketina),
            SektoreIzenak.Salmentak => SortuKargoenZerrenda((int)EnpresakoSektorea.Salmentak),
            _ => []
        };
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
            foreach (var el in SortuKargoenZerrenda(sektorea.Etiketa))
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

    public static bool SektoreaEtaKargoarenIdentifikatzaileakBaliozkoa(int sektorearenIdentifikatzailea, int kargoarenIdentifikatzailea) =>
        sektorearenIdentifikatzailea > 0 && kargoarenIdentifikatzailea > 0 &&
        KargoakSektorearekinBatDator(sektorearenIdentifikatzailea, kargoarenIdentifikatzailea);

    public static bool KargoakSektorearekinBatDator(string? sektorea, int kargoarenIdentifikatzailea)
    {
        if (string.IsNullOrWhiteSpace(sektorea) || kargoarenIdentifikatzailea <= 0)
            return false;

        var k = (EnpresakoLangileKargoa)kargoarenIdentifikatzailea;
        var esperoDenSektorea = LortuSektoreaKargoarentzat(k);
        return string.Equals(esperoDenSektorea, sektorea, StringComparison.Ordinal);
    }

    public static bool KargoakSektorearekinBatDator(int sektorearenIdentifikatzailea, int kargoarenIdentifikatzailea)
    {
        if (sektorearenIdentifikatzailea <= 0 || kargoarenIdentifikatzailea <= 0)
            return false;

        var k = (EnpresakoLangileKargoa)kargoarenIdentifikatzailea;
        var esperoDenSektorea = LortuSektoreaKargoarentzat(k);
        return string.Equals(esperoDenSektorea, LortuSektorearenEtiketa(sektorearenIdentifikatzailea), StringComparison.Ordinal);
    }

    public static bool SaiatuLeheneratuTestutik(string? kargoarenTestuaZaharra, ref string? sektorea, ref int kargoarenIdentifikatzailea)
    {
        if (string.IsNullOrWhiteSpace(kargoarenTestuaZaharra) || kargoarenIdentifikatzailea > 0)
            return false;

        var garbia = kargoarenTestuaZaharra.Trim();
        foreach (var sek in SektoreenZerrenda)
        {
            foreach (var el in SortuKargoenZerrenda(sek.Etiketa))
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

    public static bool SaiatuLeheneratuTestutik(string? kargoarenTestuaZaharra, ref int sektorearenIdentifikatzailea, ref int kargoarenIdentifikatzailea)
    {
        string? sektorea = LortuSektorearenEtiketa(sektorearenIdentifikatzailea);
        if (!SaiatuLeheneratuTestutik(kargoarenTestuaZaharra, ref sektorea, ref kargoarenIdentifikatzailea))
            return false;

        foreach (var sek in SektoreenZerrenda)
        {
            if (!string.Equals(sek.Etiketa, sektorea, StringComparison.Ordinal))
                continue;
            sektorearenIdentifikatzailea = sek.Identifikatzailea;
            return true;
        }

        sektorearenIdentifikatzailea = (int)EnpresakoSektorea.EzDaZehaztu;
        return true;
    }
}
