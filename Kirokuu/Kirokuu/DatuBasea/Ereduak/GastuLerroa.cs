using SQLite;

namespace Kirokuu.DatuBasea.Ereduak;

[Table("GastuLerroak")]
public sealed class GastuLerroa
{
    [PrimaryKey, AutoIncrement]
    [Column("GastuId")]
    public int GastuId { get; set; }

    [Column("TxostenId")]
    public int TxostenId { get; set; }

    [Column("KategoriaId")]
    public int KategoriaId { get; set; }

    [NotNull]
    public string GastuData { get; set; } = string.Empty;

    [NotNull]
    public string GarraioBidea { get; set; } = string.Empty;

    [Column("Zenbatekoa_Guztira")]
    public double ZenbatekoaGuztira { get; set; }

    public double Kilometroak { get; set; }

    [NotNull]
    [Column("TicketArgazkiBidea")]
    public string TicketArgazkia { get; set; } = string.Empty;

    [NotNull]
    public string Oharrak { get; set; } = string.Empty;

    public int KontzeptuId { get; set; }
}
