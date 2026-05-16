using Kirokuu.DatuBasea.Ereduak;

namespace Kirokuu.DatuEreduak;

public sealed class TxartelXehetasunIkuspegia
{
    public string Helmuga { get; init; } = string.Empty;

    public string SailarenEtiketa { get; init; } = string.Empty;

    public string Egoera { get; init; } = string.Empty;

    public string LangileTestua { get; init; } = string.Empty;

    public string Deskribapena { get; init; } = string.Empty;

    public string DataTestua { get; init; } = string.Empty;

    public double GastuenGuztira { get; init; }

    public string? AdminOharra { get; init; }

    public bool AdminOharraIkagarri { get; init; }

    public string? ArgazkiUrl { get; init; }

    public bool ArgazkiDago { get; init; }

    public string GarraioBideaTestua { get; init; } = string.Empty;

    public bool GarraioBideaIkagarri { get; init; }

    public bool IbilgailuaXehetasunakIkagarri { get; init; }

    public string IbilgailuaMotaTestua { get; init; } = string.Empty;

    public bool KilometroakBistaratzeaIkagarri { get; init; }

    public string KilometroakBistaratzea { get; init; } = string.Empty;

    public bool OnarpenEkintzakIkagarri { get; init; }

    public int JasoAurrerakina { get; init; }

    public bool JasoAurrerakinaIkagarri { get; init; }

    public double OrdaintzekoBidea => GastuenGuztira - JasoAurrerakina;

    public bool IbilgailuaEremuakIkagarri { get; init; }

    public bool IbilgailuaEremuakEditagarri { get; init; }

    public bool EnpresakoIbilgailua { get; init; }

    public string KilometroakTestua { get; init; } = string.Empty;

    public IReadOnlyList<GastuLerroa> GastuLerroak { get; init; } = Array.Empty<GastuLerroa>();

    public int TxostenaErabiltzaileId { get; init; }
}
