using System.Globalization;
using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.DatuEreduak;
using Kirokuu.Zerbitzuak;

namespace Kirokuu.AplikazioZerbitzuak;

public static class TursoLerroMapatzailea
{
    private const string LibsqlKateHutsarenOrdezkoa = "\u2060";

    public static Dictionary<string, string> SortuTursoLerroMapa(IReadOnlyList<string> zutabeak, IReadOnlyList<string> lerroa)
    {
        var mapa = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < zutabeak.Count && i < lerroa.Count; i++)
            mapa[zutabeak[i]] = TursoTestuaIrakurri(lerroa[i]);

        return mapa;
    }

    public static BidaiaTxostena? MapeatuBidaiaTxostenaLehena(TursoHttpExekuzioarenEmaitza emaitza)
    {
        var lerroa = emaitza.LerroTestuBalioak.FirstOrDefault();
        if (lerroa is null)
            return null;

        var mapa = SortuTursoLerroMapa(emaitza.ZutabeIzenak, lerroa);
        return MapeatuBidaiaTxostenaMapatik(mapa);
    }

    public static BidaiaTxostena MapeatuBidaiaTxostenaMapatik(Dictionary<string, string> mapa)
    {
        var adminOharraBalioa = IrakurriMapaTestuaLehenetsia(mapa, "AdminOharra", string.Empty);
        var adminDniBalioa = IrakurriMapaTestuaLehenetsia(mapa, "AdminDNI", string.Empty);
        return new BidaiaTxostena
        {
            TxostenId = IrakurriMapaOsoaLehenetsia(mapa, "TxostenId", 0),
            ErabiltzaileId = IrakurriMapaOsoaLehenetsia(mapa, "ErabiltzaileId", 0),
            LangileDNI = IrakurriMapaTestuaLehenetsia(mapa, "LangileDNI"),
            Saila = IrakurriMapaTestuaLehenetsia(mapa, "Saila"),
            Helmuga = IrakurriMapaTestuaLehenetsia(mapa, "Helmuga"),
            BidaiaHelburua = IrakurriMapaTestuaLehenetsia(mapa, "BidaiaHelburua"),
            HasieraData = DataOrduaBalioak.MapatikDataOrdua(mapa, "HasieraData"),
            AmaieraData = DataOrduaBalioak.MapatikDataOrdua(mapa, "AmaieraData"),
            PertsonaKopurua = IrakurriMapaOsoaLehenetsia(mapa, "PertsonaKopurua", 0),
            JasoAurrekina = IrakurriMapaOsoaLehenetsia(mapa, "JasoAurrerakina", 0),
            Egoera = IrakurriMapaTestuaLehenetsia(mapa, "Egoera"),
            AdminOharra = string.IsNullOrEmpty(adminOharraBalioa) ? null : adminOharraBalioa,
            AdminDNI = string.IsNullOrEmpty(adminDniBalioa) ? null : adminDniBalioa,
            EmpresaIbilgailua = IrakurriMapaOsoaLehenetsia(mapa, "EmpresaIbilgailua", 0),
            MonetaKodea = IrakurriMapaTestuaLehenetsia(mapa, "MonetaKodea"),
            SorkuntzaData = IrakurriMapaTestuaLehenetsia(mapa, "SorkuntzaData"),
            AzkenEguneratzea = IrakurriMapaTestuaLehenetsia(mapa, "AzkenEguneraketa"),
            DataAprobazioa = IrakurriMapaTestuaLehenetsia(mapa, "DataAprobazioa")
        };
    }

    public static IReadOnlyList<GastuLerroa> MapeatuGastuLerroZerrenda(TursoHttpExekuzioarenEmaitza emaitza)
    {
        var zutabeak = emaitza.ZutabeIzenak;
        var zerrenda = new List<GastuLerroa>();
        foreach (var lerroa in emaitza.LerroTestuBalioak)
        {
            var mapa = SortuTursoLerroMapa(zutabeak, lerroa);
            zerrenda.Add(new GastuLerroa
            {
                GastuId = IrakurriMapaOsoaLehenetsia(mapa, "GastuId", 0),
                TxostenId = IrakurriMapaOsoaLehenetsia(mapa, "TxostenId", 0),
                KategoriaId = IrakurriMapaOsoaLehenetsia(mapa, "KategoriaId", 0),
                GastuData = DataOrduaBalioak.MapatikDataOrdua(mapa, "GastuData"),
                GarraioBidea = IrakurriMapaTestuaLehenetsia(mapa, "GarraioBidea"),
                ZenbatekoaGuztira = IrakurriMapaKomaHamarkatuaLehenetsia(mapa, "Zenbatekoa_Guztira", 0),
                Kilometroak = IrakurriMapaKomaHamarkatuaLehenetsia(mapa, "Kilometroak", 0),
                TicketArgazkia = IrakurriMapaTestuaLehenetsia(mapa, "TicketArgazkiBidea"),
                Oharrak = IrakurriMapaTestuaLehenetsia(mapa, "Oharrak"),
                KontzeptuId = IrakurriMapaOsoaLehenetsia(mapa, "KontzeptuId", 0),
                IbilgailuaBeharrezkoa = IrakurriMapaOsoaLehenetsia(mapa, "IbilgailuaBeharrezkoa", 0)
            });
        }

        return zerrenda;
    }

    public static IReadOnlyList<GastuKontzeptua> MapeatuGastuKontzeptuZerrenda(TursoHttpExekuzioarenEmaitza emaitza)
    {
        var zutabeak = emaitza.ZutabeIzenak;
        var zerrenda = new List<GastuKontzeptua>();
        foreach (var lerroa in emaitza.LerroTestuBalioak)
        {
            var mapa = SortuTursoLerroMapa(zutabeak, lerroa);
            zerrenda.Add(new GastuKontzeptua
            {
                KategoriaId = IrakurriMapaOsoaLehenetsia(mapa, "KategoriaId", 0),
                Izena = IrakurriMapaTestuaLehenetsia(mapa, "Izena"),
                Deskribapena = IrakurriMapaTestuaLehenetsia(mapa, "Deskribapena"),
                IbilgailuaBeharDu = IrakurriMapaOsoaLehenetsia(mapa, "IbilgailuaBeharrezkoa", 0),
                Estatusa = IrakurriMapaTestuaLehenetsia(mapa, "Estatusa"),
                GastuKontzeptuId = IrakurriMapaOsoaLehenetsia(mapa, "GastuKontzeptuId", 0)
            });
        }

        return zerrenda;
    }

    public static IReadOnlyList<TxostenOnarpenLaburpena> MapeatuTxostenGuztiekLaburrak(TursoHttpExekuzioarenEmaitza emaitza)
    {
        var zutabeak = emaitza.ZutabeIzenak;
        var zerrenda = new List<TxostenOnarpenLaburpena>();
        foreach (var lerroa in emaitza.LerroTestuBalioak)
        {
            var mapa = SortuTursoLerroMapa(zutabeak, lerroa);
            zerrenda.Add(new TxostenOnarpenLaburpena
            {
                TxostenId = IrakurriMapaOsoaLehenetsia(mapa, "TxostenId", 0),
                ErabiltzaileId = IrakurriMapaOsoa(mapa, "ErabiltzaileId"),
                LangileTestua = IrakurriMapaTestuaLehenetsia(mapa, "LangileTestua"),
                Helmuga = IrakurriMapaTestuaLehenetsia(mapa, "Helmuga"),
                Egoera = IrakurriMapaTestuaLehenetsia(mapa, "Egoera"),
                GastuenBatuketakoZenbatekoa = IrakurriMapaKomaHamarkatuaLehenetsia(mapa, "GastuenBatuketakoZenbatekoa", 0),
                HasieraData = DataOrduaBalioak.MapatikDataOrdua(mapa, "HasieraData"),
                AdminTestua = IrakurriMapaTestuaLehenetsia(mapa, "AdminTestua")
            });
        }

        return zerrenda;
    }

    public static IReadOnlyList<TxostenOnarpenLaburpena> MapeatuTxostenOnarpenLaburrak(
        TursoHttpExekuzioarenEmaitza emaitza,
        string? egoeraIragazkia)
    {
        var zutabeak = emaitza.ZutabeIzenak;
        var zerrenda = new List<TxostenOnarpenLaburpena>();
        var egoeraBerretsia = string.IsNullOrWhiteSpace(egoeraIragazkia) ? null : egoeraIragazkia.Trim();
        foreach (var lerroa in emaitza.LerroTestuBalioak)
        {
            var mapa = SortuTursoLerroMapa(zutabeak, lerroa);
            var laburpena = new TxostenOnarpenLaburpena
            {
                TxostenId = IrakurriMapaOsoaLehenetsia(mapa, "TxostenId", 0),
                ErabiltzaileId = IrakurriMapaOsoa(mapa, "ErabiltzaileId"),
                LangileTestua = IrakurriMapaTestuaLehenetsia(mapa, "LangileTestua"),
                Helmuga = IrakurriMapaTestuaLehenetsia(mapa, "Helmuga"),
                Egoera = IrakurriMapaTestuaLehenetsia(mapa, "Egoera"),
                GastuenBatuketakoZenbatekoa = IrakurriMapaKomaHamarkatuaLehenetsia(mapa, "GastuenBatuketakoZenbatekoa", 0),
                HasieraData = DataOrduaBalioak.MapatikDataOrdua(mapa, "HasieraData")
            };
            if (egoeraBerretsia is not null)
                laburpena.Egoera = egoeraBerretsia;
            zerrenda.Add(laburpena);
        }

        return zerrenda;
    }

    private static string TursoTestuaIrakurri(string? gordeta)
    {
        if (string.IsNullOrEmpty(gordeta))
            return string.Empty;

        return gordeta == LibsqlKateHutsarenOrdezkoa ? string.Empty : gordeta;
    }

    private static string IrakurriMapaTestua(Dictionary<string, string> mapa, string gakoa) =>
        TursoTestuaIrakurri(mapa.TryGetValue(gakoa, out var balioa) ? balioa : throw new KeyNotFoundException(gakoa));

    private static int IrakurriMapaOsoa(Dictionary<string, string> mapa, string gakoa) =>
        int.Parse(IrakurriMapaTestua(mapa, gakoa), CultureInfo.InvariantCulture);

    public static string IrakurriMapaTestuaLehenetsia(Dictionary<string, string> mapa, string gakoa, string lehenetsia = "") =>
        mapa.TryGetValue(gakoa, out var balioa) ? TursoTestuaIrakurri(balioa) : lehenetsia;

    public static int IrakurriMapaOsoaLehenetsia(Dictionary<string, string> mapa, string gakoa, int lehenetsia = 0) =>
        mapa.TryGetValue(gakoa, out var testua) &&
        int.TryParse(testua, NumberStyles.Integer, CultureInfo.InvariantCulture, out var balioa)
            ? balioa
            : lehenetsia;

    public static double IrakurriMapaKomaHamarkatuaLehenetsia(Dictionary<string, string> mapa, string gakoa, double lehenetsia)
    {
        if (!mapa.TryGetValue(gakoa, out var testua) || string.IsNullOrWhiteSpace(testua))
            return lehenetsia;

        return double.TryParse(testua, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : lehenetsia;
    }
}
