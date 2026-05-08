using SQLite;

namespace Kirokuu.DatuBasea.Ereduak;

[Table("DiruSarrerak")]
public sealed class DiruSarrera
{
    [PrimaryKey]
    [Column("SarreraId")]
    public string SarreraId { get; set; } = string.Empty;

    [Column("ErabiltzaileId")]
    public int ErabiltzaileId { get; set; }

    public double Zenbatekoa { get; set; }

    [NotNull]
    public string Deskribapena { get; set; } = string.Empty;

    [NotNull]
    public string DataTestua { get; set; } = string.Empty;

    [NotNull]
    public string Egoera { get; set; } = string.Empty;

    public string? AdminOharra { get; set; }
}
