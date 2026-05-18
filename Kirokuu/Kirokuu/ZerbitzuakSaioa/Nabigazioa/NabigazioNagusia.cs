using Kirokuu.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace Kirokuu.ZerbitzuakSaioa;

public sealed class NabigazioNagusia : INabigazioNagusia
{
    private readonly IServiceProvider _zerbitzuHornitzailea;

    public NabigazioNagusia(IServiceProvider zerbitzuHornitzailea)
    {
        _zerbitzuHornitzailea = zerbitzuHornitzailea ?? throw new ArgumentNullException(nameof(zerbitzuHornitzailea));
    }

    public Task JoanAppShelleraAsync(CancellationToken cancellationToken = default) =>
        MainThread.InvokeOnMainThreadAsync(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var shell = _zerbitzuHornitzailea.GetRequiredService<AppShell>();
            var eraikitzailea = _zerbitzuHornitzailea.GetRequiredService<ShellFitxaEraikitzailea>();
            await eraikitzailea.KargatuAsync(shell, cancellationToken).ConfigureAwait(true);

            var leihoa = Application.Current?.Windows.FirstOrDefault();
            if (leihoa is null)
                throw new InvalidOperationException("Ez dago leiho aktiborik.");

            leihoa.Page = shell;
        });

    public Task JoanSaioHasieraraAsync(CancellationToken cancellationToken = default) =>
        MainThread.InvokeOnMainThreadAsync(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var saioHasieraOrria = _zerbitzuHornitzailea.GetRequiredService<SaioHasieraOrria>();
            var leihoa = Application.Current?.Windows.FirstOrDefault();
            if (leihoa is null)
                throw new InvalidOperationException("Ez dago leiho aktiborik.");

            leihoa.Page = new NavigationPage(saioHasieraOrria);
        });
}
