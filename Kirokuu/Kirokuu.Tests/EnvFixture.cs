using Kirokuu.Zerbitzuak;

namespace Kirokuu.Tests;

/// <summary>
/// Loads the repo <c>.env</c> once per test class instance (via <see cref="IClassFixture{T}"/>).
/// </summary>
public sealed class EnvFixture
{
    public EnvFixture()
    {
        if (!InguruneKargatzailea.KargatuDotEnv())
            throw new InvalidOperationException(
                "Could not find a .env file by walking up from the test output directory. " +
                "Ensure .env exists at the repository root (parent of the Kirokuu solution folder).");
    }
}
