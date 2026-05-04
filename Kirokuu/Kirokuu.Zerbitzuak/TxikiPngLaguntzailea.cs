namespace Kirokuu.Zerbitzuak;

/// <summary>
/// Minimal 1x1 transparent PNG bytes for lightweight upload checks.
/// </summary>
public static class TxikiPngLaguntzailea
{
    public static ReadOnlyMemory<byte> Bytes { get; } = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
}
