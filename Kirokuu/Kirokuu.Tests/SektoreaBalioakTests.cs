using Kirokuu.Zerbitzuak;

namespace Kirokuu.Tests;

public sealed class SektoreaBalioakTests
{
    [Theory]
    [InlineData(1, SektoreaBalioak.Finantzak)]
    [InlineData(2, SektoreaBalioak.Marketina)]
    [InlineData(3, SektoreaBalioak.Salmentak)]
    public void LortuSektorearenEtiketa_IdentifikatzaileBaliozkoa_EmakTestua(int id, string espero)
    {
        Assert.Equal(espero, SektoreaBalioak.LortuSektorearenEtiketa(id));
    }

    [Fact]
    public void LortuSektorearenEtiketa_IdentifikatzaileEzezaguna_Hutsik()
    {
        Assert.Equal(string.Empty, SektoreaBalioak.LortuSektorearenEtiketa(99));
    }
}
