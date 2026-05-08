namespace Kirokuu.Zerbitzuak;

/// <summary>
/// Turso HTTP pipeline / Libsql exekuzioren erantzuna bateratzen duen modelo sinplea (zutabe/lerro testu-balioetan).
/// </summary>
public sealed class TursoHttpExekuzioarenEmaitza
{
    public TursoHttpExekuzioarenEmaitza(
        IReadOnlyList<string> zutabeIzenak,
        IReadOnlyList<IReadOnlyList<string>> lerroTestuBalioak,
        long azkenTxertatutakoErrenkadaId,
        long eragindakoErrenkadaKopurua)
    {
        ZutabeIzenak = zutabeIzenak;
        LerroTestuBalioak = lerroTestuBalioak;
        AzkenTxertatutakoErrenkadaId = azkenTxertatutakoErrenkadaId;
        EragindakoErrenkadaKopurua = eragindakoErrenkadaKopurua;
    }

    public IReadOnlyList<string> ZutabeIzenak { get; }

    public IReadOnlyList<IReadOnlyList<string>> LerroTestuBalioak { get; }

    public long AzkenTxertatutakoErrenkadaId { get; }

    public long EragindakoErrenkadaKopurua { get; }
}
