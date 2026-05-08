#if ANDROID
using Android.Util;
using Microsoft.Extensions.Logging;

namespace Kirokuu;

internal sealed class LogcatLogatzaileHornitzailea : ILoggerProvider
{
    public ILogger CreateLogger(string kategoria) => new LogcatLogatzailea(kategoria);

    public void Dispose()
    {
    }
}

internal sealed class LogcatLogatzailea : ILogger
{
    private const string LogcatEtiketa = "Kirokuu";

    private readonly string _kategoria;

    public LogcatLogatzailea(string kategoria)
    {
        _kategoria = kategoria;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel maila) => maila != LogLevel.None;

    public void Log<TState>(
        LogLevel maila,
        EventId gertaeraId,
        TState egoera,
        Exception? salbuespena,
        Func<TState, Exception?, string> formatuak)
    {
        if (!IsEnabled(maila))
            return;

        var mezua = formatuak(egoera, salbuespena);
        if (salbuespena is not null)
            mezua += Environment.NewLine + salbuespena;

        var lerroa = "[" + _kategoria + "] " + mezua;

        var lehentasuna = maila switch
        {
            LogLevel.Trace or LogLevel.Debug => LogPriority.Debug,
            LogLevel.Information => LogPriority.Info,
            LogLevel.Warning => LogPriority.Warn,
            LogLevel.Error or LogLevel.Critical => LogPriority.Error,
            _ => LogPriority.Info,
        };

        global::Android.Util.Log.WriteLine(lehentasuna, LogcatEtiketa, lerroa);
    }
}
#endif
