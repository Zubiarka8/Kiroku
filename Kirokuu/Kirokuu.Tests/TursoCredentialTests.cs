using Kirokuu.Zerbitzuak;
using Xunit;

namespace Kirokuu.Tests;

[Trait("Category", "Integration")]
public sealed class TursoCredentialTests : IClassFixture<EnvFixture>
{
    public TursoCredentialTests(EnvFixture _)
    {
    }

    [Fact]
    public async Task Turso_SelectOne_Succeeds()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TURSO_DATABASE_URL")))
            Assert.Fail("TURSO_DATABASE_URL is missing after loading .env.");

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TURSO_AUTH_TOKEN")))
            Assert.Fail("TURSO_AUTH_TOKEN is missing after loading .env.");

        await TursoKonexioProbatzailea.EgiaztatuSelectBatAsync();
    }
}
