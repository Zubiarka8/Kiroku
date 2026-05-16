using System.Globalization;
using Kirokuu.AplikazioZerbitzuak;
using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.DatuEreduak;
using Kirokuu.Zerbitzuak;

namespace Kirokuu.Laguntzaileak;

public static class TxartelXehetasunLaguntzailea
{
    public static bool IbilgailuaErabiltzenDu(IReadOnlyList<GastuLerroa> lerroak) =>
        lerroak.Any(l => GarraioBideaBalioak.IbilgailuaErabiltzenDu(l.GarraioBidea));

    public static bool KilometroakIkagarri(IReadOnlyList<GastuLerroa> lerroak) =>
        lerroak.Any(l => l.Kilometroak > 0);

    public static double KilometroakGuztira(IReadOnlyList<GastuLerroa> lerroak) =>
        lerroak.Sum(l => l.Kilometroak);

    public static TxartelXehetasunIkuspegia EraikiIkuspegia(
        BidaiaTxostena txostena,
        IReadOnlyList<GastuLerroa> lerroak,
        string langileTestua = "",
        bool administratzaileIkuspegia = false)
    {
        double guztira = 0;
        double kilometroMax = 0;
        string? argazkia = null;
        var deskribapena = string.Empty;
        var ibilgailuaBeharDu = false;
        var garraioPribatua = false;
        var garraioPublikoaHautatua = false;
        var garraioBideaTestua = string.Empty;
        var garraioBideaIkagarri = false;

        foreach (var lerroa in lerroak)
        {
            guztira += lerroa.ZenbatekoaGuztira;
            if (lerroa.Kilometroak > kilometroMax)
                kilometroMax = lerroa.Kilometroak;
            if (lerroa.IbilgailuaBeharrezkoa == 1)
                ibilgailuaBeharDu = true;
            if (GarraioBideaBalioak.IbilgailuaErabiltzenDu(lerroa.GarraioBidea))
                garraioPribatua = true;
            if (string.Equals(lerroa.GarraioBidea.Trim(), GarraioBideaBalioak.GarraioPublikoa, StringComparison.Ordinal))
                garraioPublikoaHautatua = true;
            if (string.IsNullOrWhiteSpace(argazkia) && !string.IsNullOrWhiteSpace(lerroa.TicketArgazkia))
                argazkia = lerroa.TicketArgazkia;
            if (string.IsNullOrWhiteSpace(deskribapena) && !string.IsNullOrWhiteSpace(lerroa.Oharrak))
                deskribapena = lerroa.Oharrak;
            if (!garraioBideaIkagarri && !string.IsNullOrWhiteSpace(lerroa.GarraioBidea))
            {
                garraioBideaTestua = lerroa.GarraioBidea.Trim();
                garraioBideaIkagarri = true;
            }
        }

        var ibilgailuaXehetasunakIkagarri = garraioPribatua || txostena.EmpresaIbilgailua == 1 || kilometroMax > 0
            || (ibilgailuaBeharDu && !garraioPublikoaHautatua);

        var ibilgailuaMotaTestua = GarraioBideaBalioak.IbilgailuaMotaEtiketa(
            garraioBideaIkagarri ? garraioBideaTestua : lerroak.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l.GarraioBidea))?.GarraioBidea,
            txostena.EmpresaIbilgailua);
        if (string.IsNullOrWhiteSpace(ibilgailuaMotaTestua) && ibilgailuaXehetasunakIkagarri)
        {
            ibilgailuaMotaTestua = txostena.EmpresaIbilgailua == 1
                ? GarraioBideaBalioak.EnpresakoIbilgailua
                : GarraioBideaBalioak.NorberarenIbilgailua;
        }

        var onarpenEkintzakIkagarri = administratzaileIkuspegia &&
            string.Equals(txostena.Egoera, TxostenEgoera.Zain, StringComparison.Ordinal);

        var ibilgailuaEremuakIkagarri = garraioPribatua || txostena.EmpresaIbilgailua == 1 || kilometroMax > 0
            || (ibilgailuaBeharDu && !garraioPublikoaHautatua);

        var kilometroakTestua = kilometroMax > 0
            ? kilometroMax.ToString(administratzaileIkuspegia ? "0.##" : "N0", CultureInfo.InvariantCulture)
            : string.Empty;

        return new TxartelXehetasunIkuspegia
        {
            Helmuga = txostena.Helmuga,
            SailarenEtiketa = SektoreaKargoarenHiztegia.LortuBaliozkotutakoSailaTestua(txostena.Saila),
            Egoera = txostena.Egoera,
            LangileTestua = langileTestua,
            Deskribapena = deskribapena,
            DataTestua = DataOrduaBalioak.DataOrduaBistaratu(txostena.HasieraData),
            GastuenGuztira = guztira,
            AdminOharra = txostena.AdminOharra,
            AdminOharraIkagarri = !string.IsNullOrWhiteSpace(txostena.AdminOharra),
            ArgazkiUrl = argazkia,
            ArgazkiDago = !string.IsNullOrWhiteSpace(argazkia),
            GarraioBideaTestua = garraioBideaTestua,
            GarraioBideaIkagarri = garraioBideaIkagarri,
            IbilgailuaXehetasunakIkagarri = ibilgailuaXehetasunakIkagarri,
            IbilgailuaMotaTestua = ibilgailuaMotaTestua,
            KilometroakBistaratzeaIkagarri = kilometroMax > 0,
            KilometroakBistaratzea = kilometroakTestua,
            OnarpenEkintzakIkagarri = onarpenEkintzakIkagarri,
            JasoAurrerakina = txostena.JasoAurrekina,
            JasoAurrerakinaIkagarri = txostena.JasoAurrekina > 0,
            IbilgailuaEremuakIkagarri = ibilgailuaEremuakIkagarri,
            IbilgailuaEremuakEditagarri = onarpenEkintzakIkagarri && ibilgailuaEremuakIkagarri,
            EnpresakoIbilgailua = txostena.EmpresaIbilgailua == 1
                || lerroak.Any(l => GarraioBideaBalioak.DaEnpresakoIbilgailua(l.GarraioBidea)),
            KilometroakTestua = kilometroakTestua,
            GastuLerroak = lerroak,
            TxostenaErabiltzaileId = txostena.ErabiltzaileId
        };
    }
}
