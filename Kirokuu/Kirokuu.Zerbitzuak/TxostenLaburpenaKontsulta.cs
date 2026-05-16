namespace Kirokuu.Zerbitzuak;

public static class TxostenLaburpenaKontsulta
{
    public static string SortuSqlOinarria(bool adminTestuaSartu) =>
        adminTestuaSartu
            ? """
              SELECT
                b.TxostenId AS TxostenId,
                b.ErabiltzaileId AS ErabiltzaileId,
                TRIM(COALESCE(e.Izena,'') || ' ' || COALESCE(e.Abizena,'')) AS LangileTestua,
                b.Helmuga AS Helmuga,
                b.Egoera AS Egoera,
                COALESCE((SELECT SUM(gl.Zenbatekoa_Guztira) FROM GastuLerroak gl WHERE gl.TxostenId = b.TxostenId), 0) AS GastuenBatuketakoZenbatekoa,
                b.HasieraData AS HasieraData,
                COALESCE(TRIM(COALESCE(a.Izena,'') || ' ' || COALESCE(a.Abizena,'')), '') AS AdminTestua
              FROM BidaiaTxostenak b
              INNER JOIN Erabiltzaileak e ON e.ErabiltzaileId = b.ErabiltzaileId
              LEFT JOIN Erabiltzaileak a ON a.DNI = b.AdminDNI
              """
            : """
              SELECT
                b.TxostenId AS TxostenId,
                b.ErabiltzaileId AS ErabiltzaileId,
                TRIM(COALESCE(e.Izena,'') || ' ' || COALESCE(e.Abizena,'')) AS LangileTestua,
                b.Helmuga AS Helmuga,
                b.Egoera AS Egoera,
                COALESCE((SELECT SUM(gl.Zenbatekoa_Guztira) FROM GastuLerroak gl WHERE gl.TxostenId = b.TxostenId), 0) AS GastuenBatuketakoZenbatekoa,
                b.HasieraData AS HasieraData
              FROM BidaiaTxostenak b
              INNER JOIN Erabiltzaileak e ON e.ErabiltzaileId = b.ErabiltzaileId
              """;

    public static (string Sql, object[] Parametroak) SortuZerrendaSql(
        bool adminTestuaSartu,
        string? egoeraIragazkia,
        int? sektoreId,
        int? erabiltzaileId,
        bool ordenatuSorkuntzaData)
    {
        var sqlOinarria = SortuSqlOinarria(adminTestuaSartu);
        var parametroak = new List<object>();
        var baldintzak = new List<string>();

        if (erabiltzaileId is > 0)
        {
            baldintzak.Add("b.ErabiltzaileId = ?");
            parametroak.Add(erabiltzaileId.Value);
        }

        if (!string.IsNullOrWhiteSpace(egoeraIragazkia))
        {
            baldintzak.Add("b.Egoera = ?");
            parametroak.Add(egoeraIragazkia.Trim());
        }

        if (sektoreId is > 0)
        {
            baldintzak.Add("e.Sektorea = ?");
            parametroak.Add(SektoreaBalioak.LortuSektorearenEtiketa(sektoreId.Value));
        }

        var sql = sqlOinarria;
        if (baldintzak.Count > 0)
            sql += " WHERE " + string.Join(" AND ", baldintzak);

        sql += ordenatuSorkuntzaData
            ? " ORDER BY b.SorkuntzaData DESC"
            : " ORDER BY b.HasieraData DESC";

        return (sql, parametroak.ToArray());
    }
}
