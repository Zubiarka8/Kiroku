using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.Zerbitzuak;

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

    public string DataFormateatua => DataOrduaBalioak.DataOrduaBistaratu(HasieraData);

    public bool EzeztatuDaiteke => string.Equals(Egoera, TxostenEgoera.Zain, StringComparison.Ordinal);

    public string AdminTestua { get; set; } = string.Empty;

    public bool AdminTestuaIkagarri => !string.IsNullOrWhiteSpace(AdminTestua);
}
