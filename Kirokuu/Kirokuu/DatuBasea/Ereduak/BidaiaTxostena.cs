using SQLite;

namespace Kirokuu.DatuBasea.Ereduak;

[Table("BidaiaTxostenak")]
public sealed class BidaiaTxostena
{
    [PrimaryKey, AutoIncrement]
    public int TxostenId { get; set; }

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

    public int JasoAurrerakina { get; set; }

    [NotNull]
    public string Egoera { get; set; } = string.Empty;

    public string? AdminOharra { get; set; }

    public string? AdminDNI { get; set; }

    public int EmpresaIbilgailua { get; set; }

    [NotNull]
    public string MonetaKodea { get; set; } = "EUR";

    [NotNull]
    public string SorkuntzaData { get; set; } = string.Empty;

    [NotNull]
    public string AzkenEguneraketa { get; set; } = string.Empty;

    [NotNull]
    public string DataAprobazioa { get; set; } = string.Empty;
}
