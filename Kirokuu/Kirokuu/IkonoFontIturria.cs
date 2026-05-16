namespace Kirokuu;

public static class IkonoFontIturria
{
    public const string MaterialIconsFamilia = "MaterialIcons";

    public static FontImageSource Sortu(string glifoa, double tamaina = 24, Color? kolorea = null)
    {
        var iturria = new FontImageSource
        {
            FontFamily = MaterialIconsFamilia,
            Glyph = glifoa,
            Size = tamaina
        };
        if (kolorea is not null)
            iturria.Color = kolorea;
        return iturria;
    }

    public static FontImageSource FitxaHasieraLangile(double tamaina = 26) => Sortu("\ue88a", tamaina);

    public static FontImageSource FitxaNireTxartelak(double tamaina = 26) => Sortu("\uef6e", tamaina);

    public static FontImageSource FitxaTxartelKanban(double tamaina = 26) => Sortu("\ueb7f", tamaina);

    public static FontImageSource FitxaHasieraAdministratzaile(double tamaina = 26) => Sortu("\ue871", tamaina);

    public static FontImageSource FitxaErabiltzaileZerrenda(double tamaina = 26) => Sortu("\ue7ef", tamaina);

    public static FontImageSource FitxaMugimenduak(double tamaina = 26) => Sortu("\ue8d4", tamaina);

    public static FontImageSource FitxaTxostenGuztiak(double tamaina = 26) => Sortu("\ue241", tamaina);

    public static FontImageSource FitxaEzarpenak(double tamaina = 26) => Sortu("\ue8b8", tamaina);

    public static string HutsikIkonoGlifoa => "\ue156";

    public static string TxartelIkonoGlifoa => "\uef6e";

    public static string GehituIkonoGlifoa => "\ue145";

    public static string JoanIkonoGlifoa => "\ue5c8";

    public static string BaieztatuIkonoGlifoa => "\ue86c";

    public static string UkatuIkonoGlifoa => "\ue5c9";

    public static string ArgazkiaIkonoGlifoa => "\ue412";

    public static string DiruaIkonoGlifoa => "\ue227";

    public static string SaioHasieraIkonoGlifoa => "\uea77";

    public static string PostaArrobaIkonoGlifoa => "\ue0e6";

    public static string LasterEdukiaIkonoGlifoa => "\ue88b";

    public static string ChevronEskuinaIkonoGlifoa => "\ue5cc";
}
