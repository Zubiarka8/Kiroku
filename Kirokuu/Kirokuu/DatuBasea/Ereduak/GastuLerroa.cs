using Kirokuu.Zerbitzuak;
using SQLite;

namespace Kirokuu.DatuBasea.Ereduak;

[Table("GastuLerroak")]
public sealed class GastuLerroa
{
    [PrimaryKey, AutoIncrement]
    public int GastuId { get; set; }

    public int TxostenId { get; set; }

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

    public int IbilgailuaBeharrezkoa { get; set; }

    [Ignore]
    public string GastuDataFormateatua => DataOrduaBalioak.DataOrduaBistaratu(GastuData);

    [Ignore]
    public bool KilometroakIkagarri => Kilometroak > 0;

    [Ignore]
    public bool IbilgailuaBeharrezkoaBai => IbilgailuaBeharrezkoa == 1;

    [Ignore]
    public bool IrudiaDauka => !string.IsNullOrWhiteSpace(TicketArgazkia);

    [Ignore]
    public string? TicketArgazkiUrl => IrudiaDauka ? TicketArgazkia : null;
}
