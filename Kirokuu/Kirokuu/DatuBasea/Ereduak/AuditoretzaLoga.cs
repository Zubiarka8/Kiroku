using SQLite;

namespace Kirokuu.DatuBasea.Ereduak;

[Table("AuditoretzaLoga")]
public sealed class AuditoretzaLoga
{
    [PrimaryKey, AutoIncrement]
    [Column("LogId")]
    public int LogId { get; set; }

    [Column("DiruSarreraId")]
    public int? DiruSarreraId { get; set; }

    [Column("ErabiltzaileId")]
    public int ErabiltzaileId { get; set; }

    [NotNull]
    public string Ekintza { get; set; } = string.Empty;

    [NotNull]
    public DateTime  DataOrdua { get; set; }

    [NotNull]
    public string Deskribapena { get; set; } = string.Empty;

    [NotNull]
    [Column("IP_Helbidea")]
    public string IpHelbidea { get; set; } = string.Empty;
}
