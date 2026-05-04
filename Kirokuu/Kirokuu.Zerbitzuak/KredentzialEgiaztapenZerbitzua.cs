namespace Kirokuu.Zerbitzuak;

/// <summary>
/// Runs the same checks as integration tests: .env, Turso SELECT 1, Cloudinary tiny upload.
/// Returns a coarse outcome for UI (Spanish strings are applied in the ViewModel).
/// </summary>
public sealed class KredentzialEgiaztapenZerbitzua
{
    private readonly ArgazkiIgotzeZerbitzua _argazkiIgotzeZerbitzua;

    public KredentzialEgiaztapenZerbitzua(ArgazkiIgotzeZerbitzua argazkiIgotzeZerbitzua)
    {
        _argazkiIgotzeZerbitzua = argazkiIgotzeZerbitzua ?? throw new ArgumentNullException(nameof(argazkiIgotzeZerbitzua));
    }

    public async Task<KredentzialEgiaztapenEmaitza> EgiaztatuAsync(CancellationToken cancellationToken = default)
    {
        if (!InguruneKargatzailea.KargatuDotEnv())
        {
            return new KredentzialEgiaztapenEmaitza(
                KredentzialEgiaztapenMota.Gaizki,
                "No se encontró un archivo .env en las carpetas superiores del proceso.");
        }

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TURSO_DATABASE_URL")) ||
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TURSO_AUTH_TOKEN")))
        {
            return new KredentzialEgiaztapenEmaitza(
                KredentzialEgiaztapenMota.Gaizki,
                "Faltan TURSO_DATABASE_URL o TURSO_AUTH_TOKEN en el entorno.");
        }

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME")) ||
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CLOUDINARY_UPLOAD_PRESET")))
        {
            return new KredentzialEgiaztapenEmaitza(
                KredentzialEgiaztapenMota.Gaizki,
                "Faltan CLOUDINARY_CLOUD_NAME o CLOUDINARY_UPLOAD_PRESET en el entorno.");
        }

        try
        {
            await TursoKonexioProbatzailea.EgiaztatuSelectBatAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (LikelyAuthFailure(ex))
        {
            return new KredentzialEgiaztapenEmaitza(
                KredentzialEgiaztapenMota.Gaizki,
                BuildSafeDetail("Turso rechazó las credenciales o no tiene permiso.", ex));
        }
        catch (Exception ex)
        {
            return new KredentzialEgiaztapenEmaitza(
                KredentzialEgiaztapenMota.Errorea,
                BuildSafeDetail("Error al conectar con Turso.", ex));
        }

        try
        {
            await using var stream = new MemoryStream(TxikiPngLaguntzailea.Bytes.ToArray());
            await _argazkiIgotzeZerbitzua.IgoArgazkiaAsync(stream, "tiny.png", cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex) when (HttpStatusIndicatesUnauthorized(ex))
        {
            return new KredentzialEgiaztapenEmaitza(
                KredentzialEgiaztapenMota.Gaizki,
                BuildSafeDetail("Cloudinary rechazó la subida (no autorizado).", ex));
        }
        catch (HttpRequestException ex)
        {
            return new KredentzialEgiaztapenEmaitza(
                KredentzialEgiaztapenMota.Errorea,
                BuildSafeDetail("Error de red o de API con Cloudinary.", ex));
        }
        catch (Exception ex)
        {
            return new KredentzialEgiaztapenEmaitza(
                KredentzialEgiaztapenMota.Errorea,
                BuildSafeDetail("Error inesperado al subir a Cloudinary.", ex));
        }

        return new KredentzialEgiaztapenEmaitza(KredentzialEgiaztapenMota.Ongi, null);
    }

    private static bool HttpStatusIndicatesUnauthorized(HttpRequestException ex)
    {
        var m = ex.Message;
        return m.Contains("401", StringComparison.Ordinal) || m.Contains("403", StringComparison.Ordinal);
    }

    private static bool LikelyAuthFailure(Exception ex)
    {
        for (var current = ex; current != null; current = current.InnerException!)
        {
            var text = current.Message;
            if (text.Contains("401", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("403", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("unauthorized", StringComparison.OrdinalIgnoreCase))
                return true;

            if (current.GetType().Name.Contains("Auth", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string BuildSafeDetail(string prefix, Exception ex)
    {
        return $"{prefix} ({ex.GetType().Name})";
    }
}
