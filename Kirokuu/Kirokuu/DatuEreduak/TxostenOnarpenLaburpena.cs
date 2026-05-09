namespace Kirokuu.DatuEreduak;

public sealed class TxostenOnarpenLaburpena
{
    public int TxostenId { get; set; }

    public int ErabiltzaileId { get; set; }

    public string LangileTestua { get; set; } = string.Empty;

    public string Helmuga { get; set; } = string.Empty;

    public string Egoera { get; set; } = string.Empty;

    public double GastuenBatuketakoZenbatekoa { get; set; }

    public bool EzeztatuDaiteke => string.Equals(Egoera, "Zain", StringComparison.Ordinal);
}
