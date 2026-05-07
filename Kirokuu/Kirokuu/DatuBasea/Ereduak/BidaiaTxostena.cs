using SQLite;

namespace Kirokuu.DatuBasea.Ereduak;

[Table("BidaiaTxostenak")]
public sealed class BidaiaTxostena
{
    [PrimaryKey]
    [Column("TxostenId")]
    public string TxostenId { get; set; } = string.Empty;

    [Column("ErabiltzaileId")]
    public int ErabiltzaileId { get; set; }

    [NotNull]
    public string LangileDNI { get; set; } = string.Empty;

    [NotNull]
    public string Saila { get; set; } = string.Empty;

    [NotNull]
    public string Helmuga { get; set; } = string.Empty;

    [NotNull]
    public string BidaiaHelburua { get; set; } = string.Empty;

    [NotNull]
    public DateTime  HasieraData { get; set; }

    [NotNull]
    public DateTime  AmaieraData { get; set; }

    public int PertsonaKopurua { get; set; }

    public int JasoAurrekina { get; set; }

    [NotNull]
    public string Egoera { get; set; } = string.Empty;

    [NotNull]
    public string MonetaKodea { get; set; } = string.Empty;

    [NotNull]
    public DateTime  SorkuntzaData { get; set; }

    [NotNull]
    public DateTime  AzkenEguneratzea { get; set; }

    [NotNull]
    public DateTime  DataAprobazioa { get; set; }
}
