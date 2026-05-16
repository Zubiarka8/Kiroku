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

    [NotNull, Indexed(Unique = true)]
    [Column("DNI")]
    public string DNI { get; set; } = string.Empty;

    [NotNull, Indexed(Unique = true)]
    [Column("Email")]
    public string Posta { get; set; } = string.Empty;

    [NotNull]
    public string Kargoa { get; set; } = string.Empty;

    [NotNull]
    [Column("Sektorea")]
    public string Sektorea { get; set; } = string.Empty;

    [Ignore]
    public int SektorearenIdentifikatzailea
    {
        get => Sektorea switch
        {
            "Finantzak" => (int)EnpresakoSektorea.Finantzak,
            "Marketina" => (int)EnpresakoSektorea.Marketina,
            "Salmentak" => (int)EnpresakoSektorea.Salmentak,
            _ => (int)EnpresakoSektorea.EzDaZehaztu
        };
        set => Sektorea = value switch
        {
            (int)EnpresakoSektorea.Finantzak => "Finantzak",
            (int)EnpresakoSektorea.Marketina => "Marketina",
            (int)EnpresakoSektorea.Salmentak => "Salmentak",
            _ => string.Empty
        };
    }

    public int Rola { get; set; }

    public int Aktiboa { get; set; } = 1;

    [NotNull]
    [Column("SorkuntzaData")]
    public string SorkuntzaData { get; set; } = string.Empty;

    [NotNull]
    public string Pasahitza { get; set; } = string.Empty;

    [Column("SaioHasieraSaiakerak")]
    public int SaioHasieraSaiakerak { get; set; }

    [Column("SaioaBlokeoaAmaieraUtc")]
    public string? SaioaBlokeoaAmaieraUtc { get; set; }

}
