namespace Kirokuu.ZerbitzuakSaioa;

public static class AuditoretzaEkintzak
{
    public const string TxostenaOnartua = "TxostenaOnartua";

    public const string TxostenaEzeztatu = "TxostenaEzeztatu";

    public const string TxostenaEskatuDu = "TxostenaEskatuDu";

    public const string TxostenaBertanBehera = "TxostenaBertanBehera";
}

public sealed class AuditoretzaZerbitzua
{
    private readonly DatuBaseaZerbitzua _datuBaseaZerbitzua;

    public AuditoretzaZerbitzua(DatuBaseaZerbitzua datuBaseaZerbitzua)
    {
        _datuBaseaZerbitzua = datuBaseaZerbitzua ?? throw new ArgumentNullException(nameof(datuBaseaZerbitzua));
    }

    public Task IdazkiLogaAsync(
        string ekintza,
        string deskribapena,
        int erabiltzaileId,
        int? txostenId = null,
        int? langileId = null,
        CancellationToken cancellationToken = default) =>
        _datuBaseaZerbitzua.IdazkiAuditoretzaLogaZerbitzuraAsync(
            ekintza,
            deskribapena,
            erabiltzaileId,
            txostenId,
            langileId,
            cancellationToken);
}
