using SQLite;

namespace Kirokuu.DatuBasea.Ereduak;

[Table("Erabiltzaileak")]
public sealed class Erabiltzailea
{
    [PrimaryKey, AutoIncrement]
    [Column("ErabiltzaileId")]
    public int Id { get; set; }

    [NotNull]
    public string Izena { get; set; } = string.Empty;

    [NotNull]
    public string Abizena { get; set; } = string.Empty;

    [NotNull]
    public string Abizena2 { get; set; } = string.Empty;

    [NotNull]
    [Column("DNI")]
    public string Nan { get; set; } = string.Empty;

    [NotNull, Indexed(Unique = true)]
    [Column("Email")]
    public string Posta { get; set; } = string.Empty;

    [NotNull]
    public string Kargoa { get; set; } = string.Empty;

    public int Rola { get; set; }

    [NotNull]
    [Column("SorkuntzaData")]
    public string SorkuntzaData { get; set; } = string.Empty;

    [NotNull]
    public string PasahitzaHash { get; set; } = string.Empty;

    [NotNull]
    public string PasahitzaGatza { get; set; } = string.Empty;

    public int HutsuneakSaioan { get; set; }
}
