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
            tabBar.Items.Add(SortuAdministratzaileOrria<AdministratzaileHasieraOrria>("Hasiera"));
            tabBar.Items.Add(SortuAdministratzaileOrria<LangileZerrendaOrria>("Erabiltzaileak"));
            tabBar.Items.Add(SortuAdministratzaileOrria<MugimenduakOrria>("Mugimenduak"));
            tabBar.Items.Add(SortuAdministratzaileOrria<DiruEskaerakInformeaOrria>("Informea"));
            tabBar.Items.Add(SortuEzarpenak());
        }
        else
        {
            tabBar.Items.Add(SortuLangileOrria<NireTxartelakOrria>("Nire txartelak"));
            tabBar.Items.Add(SortuLangileOrria<TxartelKanbanOrria>("Txartelak"));
            tabBar.Items.Add(SortuEzarpenak());
        }

        shell.Items.Add(tabBar);
    }

    private ShellContent SortuAdministratzaileOrria<T>(string titulua) where T : Page
    {
        var orria = _zerbitzuHornitzailea.GetRequiredService<T>();
        return new ShellContent
        {
            Title = titulua,
            Content = orria,
            Route = typeof(T).Name
        };
    }

    private ShellContent SortuLangileOrria<T>(string titulua) where T : Page
    {
        var orria = _zerbitzuHornitzailea.GetRequiredService<T>();
        return new ShellContent
        {
            Title = titulua,
            Content = orria,
            Route = typeof(T).Name
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
