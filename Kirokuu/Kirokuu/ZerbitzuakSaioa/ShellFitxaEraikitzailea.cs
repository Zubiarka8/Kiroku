using Kirokuu.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace Kirokuu.ZerbitzuakSaioa;

public sealed class ShellFitxaEraikitzailea
{
    private readonly IServiceProvider _zerbitzuHornitzailea;
    private readonly AutorizazioZerbitzua _autorizazioZerbitzua;

    public ShellFitxaEraikitzailea(
        IServiceProvider zerbitzuHornitzailea,
        AutorizazioZerbitzua autorizazioZerbitzua)
    {
        _zerbitzuHornitzailea = zerbitzuHornitzailea ?? throw new ArgumentNullException(nameof(zerbitzuHornitzailea));
        _autorizazioZerbitzua = autorizazioZerbitzua ?? throw new ArgumentNullException(nameof(autorizazioZerbitzua));
    }

    public async Task KargatuAsync(AppShell shell, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shell);
        cancellationToken.ThrowIfCancellationRequested();

        var adminDa = await _autorizazioZerbitzua.DaAdministratzaileaAsync(cancellationToken).ConfigureAwait(true);

        shell.Items.Clear();
        var tabBar = new TabBar();

        if (adminDa)
        {
            tabBar.Items.Add(SortuLasterEdukia("Txartel guztiak", "TxartelGuztiak"));
            tabBar.Items.Add(SortuLasterEdukia("Nire gastuak", "NireGastuak"));
            tabBar.Items.Add(SortuLangileZerrenda());
            tabBar.Items.Add(SortuEzarpenak());
        }
        else
        {
            tabBar.Items.Add(SortuLasterEdukia("Nire txartelak", "NireTxartelak"));
            tabBar.Items.Add(SortuLasterEdukia("Txartel berria", "TxartelBerria"));
            tabBar.Items.Add(SortuEzarpenak());
        }

        shell.Items.Add(tabBar);
    }

    private ShellContent SortuLasterEdukia(string titulua, string ibilbidea)
    {
        var orria = _zerbitzuHornitzailea.GetRequiredService<LasterEdukiaOrria>();
        return new ShellContent
        {
            Title = titulua,
            Content = orria,
            Route = ibilbidea
        };
    }

    private ShellContent SortuLangileZerrenda()
    {
        var orria = _zerbitzuHornitzailea.GetRequiredService<LangileZerrendaOrria>();
        return new ShellContent
        {
            Title = "Langile zerrenda",
            Content = orria,
            Route = nameof(LangileZerrendaOrria)
        };
    }

    private ShellContent SortuEzarpenak()
    {
        var orria = _zerbitzuHornitzailea.GetRequiredService<EzarpenakOrria>();
        return new ShellContent
        {
            Title = "Ezarpenak",
            Content = orria,
            Route = nameof(EzarpenakOrria)
        };
    }
}
