using SQLite;

namespace Kirokuu.DatuBasea.Ereduak;

[Table("GastuKontzeptuak")]
public sealed class GastuKontzeptua
{
    [PrimaryKey]
    [Column("KategoriaId")]
    public int KategoriaId { get; set; }

    [NotNull]
    public string Izena { get; set; } = string.Empty;

    [NotNull]
    public string Deskribapena { get; set; } = string.Empty;

    [Column("IbilgailuaBeharrezkoa")]
    public int IbilgailuaBeharDu { get; set; }

    [NotNull]
    public string Estatusa { get; set; } = string.Empty;

    [Column("GastuKontzeptuId")]
    public int GastuKontzeptuId { get; set; }
}
