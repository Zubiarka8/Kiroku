namespace Kirokuu.ZerbitzuakSaioa;

/// <summary>
/// Posta bikoiztua edo murrizketa bateraezinaren adierazlea (SQLite edo Turso).
/// </summary>
public sealed class ErabiltzaileMurrizketaSalbuespena : Exception
{
    public ErabiltzaileMurrizketaSalbuespena(string mezua, Exception? barnekoSalbuespena = null)
        : base(mezua, barnekoSalbuespena)
    {
    }
}
