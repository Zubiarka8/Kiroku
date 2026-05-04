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
    public string HasieraData { get; set; } = string.Empty;

    [NotNull]
    public string AmaieraData { get; set; } = string.Empty;

    public int PertsonaKopurua { get; set; }

    public int JasoAurrekoa { get; set; }

    [NotNull]
    public string Egoera { get; set; } = string.Empty;

    [NotNull]
    public string MonetaKodea { get; set; } = string.Empty;

    [NotNull]
    public string SorkuntzaData { get; set; } = string.Empty;

    [NotNull]
    public string AzkenEguneratzea { get; set; } = string.Empty;

    [NotNull]
    public string DataAprobazioa { get; set; } = string.Empty;
}
