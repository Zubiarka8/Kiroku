using Kirokuu.Zerbitzuak;

namespace Kirokuu.Tests;

public sealed class DataOrduaBalioakTests
{
    [Fact]
    public void DataOrduaOsatu_DataSoila_UTC_OsatuDa()
    {
        var emaitza = DataOrduaBalioak.DataOrduaOsatu("2026-05-16");
        Assert.Contains("2026-05-16", emaitza);
        Assert.Contains("T", emaitza);
    }
}
