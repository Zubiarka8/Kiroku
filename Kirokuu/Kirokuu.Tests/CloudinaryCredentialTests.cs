using Kirokuu.Zerbitzuak;
using Xunit;

namespace Kirokuu.Tests;

[Trait("Category", "Integration")]
public sealed class CloudinaryCredentialTests : IClassFixture<EnvFixture>, IDisposable
{
    private readonly ArgazkiIgotzeZerbitzua _zerbitzua = new();

    public CloudinaryCredentialTests(EnvFixture _)
    {
    }

    [Fact]
    public async Task Cloudinary_TinyPng_ReturnsSecureUrl()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME")))
            Assert.Fail("CLOUDINARY_CLOUD_NAME is missing after loading .env.");

        var presetFromEnv = Environment.GetEnvironmentVariable("CLOUDINARY_UPLOAD_PRESET");
        if (string.IsNullOrWhiteSpace(presetFromEnv))
            Assert.Fail("CLOUDINARY_UPLOAD_PRESET is missing. Set it to your unsigned upload preset name in .env.");

        var stream = new MemoryStream(TxikiPngLaguntzailea.Bytes.ToArray());
        var url = await _zerbitzua.IgoArgazkiaAsync(stream, "tiny.png");

        Assert.False(string.IsNullOrWhiteSpace(url));
        Assert.StartsWith("https://", url, StringComparison.Ordinal);
        Assert.Contains("res.cloudinary.com", url, StringComparison.Ordinal);
    }

    public void Dispose() => _zerbitzua.Dispose();
}
