namespace Kirokuu.Zerbitzuak;

/// <summary>
/// Turso/libSQL konsultek exekuzioren emaitza bateratu bat itzultzen duten artegian.
/// </summary>
public interface ITursoSqlEgikaritzailea
{
    Task<TursoHttpExekuzioarenEmaitza> ExekutatuAsync(
        string sql,
        CancellationToken cancellationToken,
        params object?[] argumentuak);
}
