using Kirokuu.DatuBasea.Ereduak;

namespace Kirokuu.ZerbitzuakSaioa;

public sealed partial class DatuBaseaZerbitzua
{
    /// <summary>
    /// FCM jakinarazpen tokena gordetzen du administratzaile edo zuzendari nagusiaren errenkadan soilik.
    /// </summary>
    public async Task EguneratuJakinarazpenTokenaAsync(
        int erabiltzaileId,
        string? tokena,
        CancellationToken cancellationToken = default)
    {
        await HasieratuAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var tokenTestua = tokena ?? string.Empty;

        if (_urrunTursoModua)
        {
            await ExekutatuTursoanAsync(async bezeroa =>
            {
                const string sql = """
                    UPDATE Erabiltzaileak
                    SET JakinarazpenTokena = ?
                    WHERE ErabiltzaileId = ?
                      AND (Rola = ? OR Rola = ?);
                    """;
                await bezeroa.ExekutatuAsync(
                    sql,
                    cancellationToken,
                    LibsqlLoturaNormalizatua(tokenTestua),
                    LibsqlLoturaNormalizatua(erabiltzaileId),
                    LibsqlLoturaNormalizatua((int)ErabiltzaileRola.Administratzailea),
                    LibsqlLoturaNormalizatua((int)ErabiltzaileRola.ZuzendariNagusia)).ConfigureAwait(false);
                return 0;
            }, cancellationToken).ConfigureAwait(false);
            return;
        }

        await _sqliteKonexioa!.ExecuteAsync(
                """
                UPDATE Erabiltzaileak
                SET JakinarazpenTokena = ?
                WHERE ErabiltzaileId = ?
                  AND (Rola = ? OR Rola = ?);
                """,
                tokenTestua,
                erabiltzaileId,
                (int)ErabiltzaileRola.Administratzailea,
                (int)ErabiltzaileRola.ZuzendariNagusia)
            .ConfigureAwait(false);
    }
}
