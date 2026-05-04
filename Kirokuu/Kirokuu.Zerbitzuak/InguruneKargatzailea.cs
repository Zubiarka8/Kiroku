namespace Kirokuu.Zerbitzuak;

/// <summary>
/// Locates the repo <c>.env</c> by walking parent directories and loads it via DotNetEnv.
/// </summary>
public static class InguruneKargatzailea
{
    /// <summary>
    /// Walks from <paramref name="startDirectory"/> (or <see cref="AppContext.BaseDirectory"/>) toward the filesystem root
    /// and returns the first path where a file named <c>.env</c> exists.
    /// </summary>
    public static string? BilatuEnvFitxategiarenBidea(string? startDirectory = null)
    {
        var current = new DirectoryInfo(startDirectory ?? AppContext.BaseDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, ".env");
            if (File.Exists(candidate))
                return candidate;

            current = current.Parent;
        }

        return null;
    }

    /// <summary>
    /// Loads environment variables from the first <c>.env</c> found upward from the start directory.
    /// </summary>
    /// <returns><c>true</c> if a file was loaded; otherwise <c>false</c>.</returns>
    public static bool KargatuDotEnv(string? startDirectory = null)
    {
        var path = BilatuEnvFitxategiarenBidea(startDirectory);
        if (path is null)
            return false;

        DotNetEnv.Env.Load(path, new DotNetEnv.LoadOptions(setEnvVars: true, clobberExistingVars: true, onlyExactPath: false));
        return true;
    }
}
