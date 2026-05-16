using Kirokuu.Zerbitzuak;

namespace Kirokuu.Tests;

public sealed class TxostenLaburpenaKontsultaTests
{
    [Fact]
    public void SortuZerrendaSql_SektoreId1_ParametroaFinantzak()
    {
        var (_, parametroak) = TxostenLaburpenaKontsulta.SortuZerrendaSql(
            adminTestuaSartu: false,
            egoeraIragazkia: null,
            sektoreId: 1,
            erabiltzaileId: null,
            ordenatuSorkuntzaData: true);

        Assert.Single(parametroak);
        Assert.Equal(SektoreaBalioak.Finantzak, parametroak[0]);
    }
}
