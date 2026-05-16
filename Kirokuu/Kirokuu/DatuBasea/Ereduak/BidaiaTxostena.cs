using SQLite;

namespace Kirokuu.DatuBasea.Ereduak;

[Table("BidaiaTxostenak")]
public sealed class BidaiaTxostena
{
    [PrimaryKey, AutoIncrement]
    [Column("TxostenId")]
    public int TxostenId { get; set; }

    [Column("ErabiltzaileId")]
    public int ErabiltzaileId { get; set; }

    [NotNull]
    public string LangileDNI { get; set; } = string.Empty;

    [NotNull]
    public string Saila { get; set; } = string.Empty;

    [Ignore]
    public int SailarenIdentifikatzailea
    {
        get => Saila switch
        {
            "Finantzak" => (int)EnpresakoSektorea.Finantzak,
            "Marketina" => (int)EnpresakoSektorea.Marketina,
            "Salmentak" => (int)EnpresakoSektorea.Salmentak,
            _ => (int)EnpresakoSektorea.EzDaZehaztu
        };
        set => Saila = value switch
        {
            (int)EnpresakoSektorea.Finantzak => "Finantzak",
            (int)EnpresakoSektorea.Marketina => "Marketina",
            (int)EnpresakoSektorea.Salmentak => "Salmentak",
            _ => string.Empty
        };
    }

    [NotNull]
    public string Helmuga { get; set; } = string.Empty;

    [NotNull]
    public string BidaiaHelburua { get; set; } = string.Empty;

    [NotNull]
    public string  HasieraData { get; set; }

    [NotNull]
    public string  AmaieraData { get; set; }

    public int PertsonaKopurua { get; set; }

    [Column("JasoAurrerakina")]
    public int JasoAurrekina { get; set; }

    [NotNull]
    public string Egoera { get; set; } = string.Empty;

    public string? AdminOharra { get; set; }

    [Column("AdminDNI")]
    public string? AdminDNI { get; set; }

    [Column("EmpresaIbilgailua")]
    public int EmpresaIbilgailua { get; set; }

    [NotNull]
    public string MonetaKodea { get; set; } = string.Empty;

    [NotNull]
    public string  SorkuntzaData { get; set; }

    [NotNull]
    [Column("AzkenEguneraketa")]
    public string  AzkenEguneratzea { get; set; }

    [NotNull]
    public string  DataAprobazioa { get; set; }
}
