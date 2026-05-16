using System.Globalization;

namespace Kirokuu.DatuEreduak;

public sealed class TxostenOnarpenLaburpena
{
    public int TxostenId { get; set; }

    public int ErabiltzaileId { get; set; }

    public string LangileTestua { get; set; } = string.Empty;

    public string Helmuga { get; set; } = string.Empty;

    public string Egoera { get; set; } = string.Empty;

    public double GastuenBatuketakoZenbatekoa { get; set; }

    public string HasieraData { get; set; } = string.Empty;

    public string DataFormateatua
    {
        get
        {
            if (string.IsNullOrWhiteSpace(HasieraData))
                return string.Empty;
            if (DateTime.TryParse(HasieraData, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces, out var d))
                return d.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            return HasieraData;
        }
    }

    public bool EzeztatuDaiteke => string.Equals(Egoera, "Zain", StringComparison.Ordinal);

    public string AdminTestua { get; set; } = string.Empty;

    public bool AdminTestuaIkagarri => !string.IsNullOrWhiteSpace(AdminTestua);
}
