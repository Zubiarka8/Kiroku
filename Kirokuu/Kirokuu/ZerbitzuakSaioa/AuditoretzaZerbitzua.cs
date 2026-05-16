namespace Kirokuu.ZerbitzuakSaioa;

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
