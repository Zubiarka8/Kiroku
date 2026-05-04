using SQLite;

namespace Kirokuu.DatuBasea.Ereduak;

[Table("Erabiltzailea")]
public sealed class Erabiltzailea
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string Izena { get; set; } = string.Empty;

    [NotNull]
    public string Abizena { get; set; } = string.Empty;

    [NotNull, Indexed(Unique = true)]
    public string Posta { get; set; } = string.Empty;

    [NotNull]
    public string PasahitzaHash { get; set; } = string.Empty;

    [NotNull]
    public string PasahitzaGatza { get; set; } = string.Empty;

    public int Rola { get; set; }

    public int HutsuneakSaioan { get; set; }
}
