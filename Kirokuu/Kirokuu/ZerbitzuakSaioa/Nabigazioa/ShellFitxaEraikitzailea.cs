using Kirokuu;
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

        var administratzaileOsoaDa =
            await _autorizazioZerbitzua.DaAdministratzaileaAsync(cancellationToken).ConfigureAwait(true);
        var zuzendariNagusiaDa =
            await _autorizazioZerbitzua.DaZuzendariNagusiaAsync(cancellationToken).ConfigureAwait(true);

        shell.Items.Clear();
        var tabBar = new TabBar();

        if (administratzaileOsoaDa)
        {
            tabBar.Items.Add(SortuAdministratzaileOrria<AdministratzaileHasieraOrria>("Hasiera", IkonoFontIturria.FitxaHasieraAdministratzaile()));
            tabBar.Items.Add(SortuAdministratzaileOrria<LangileZerrendaOrria>("Erabiltzaileak", IkonoFontIturria.FitxaErabiltzaileZerrenda()));
            tabBar.Items.Add(SortuAdministratzaileOrria<TxostenGuztiekOrria>("Txosten guztiak", IkonoFontIturria.FitxaTxostenGuztiak()));
            tabBar.Items.Add(SortuEzarpenak());
        }
        else if (zuzendariNagusiaDa)
        {
            tabBar.Items.Add(SortuAdministratzaileOrria<AdministratzaileHasieraOrria>("Hasiera", IkonoFontIturria.FitxaHasieraAdministratzaile()));
            tabBar.Items.Add(SortuAdministratzaileOrria<LangileZerrendaOrria>("Erabiltzaileak", IkonoFontIturria.FitxaErabiltzaileZerrenda()));
            tabBar.Items.Add(SortuAdministratzaileOrria<TxostenGuztiekOrria>("Txosten guztiak", IkonoFontIturria.FitxaTxostenGuztiak()));
            tabBar.Items.Add(SortuEzarpenak());
        }
        else
        {
            tabBar.Items.Add(SortuLangileOrria<HasieraOrria>("Hasiera", IkonoFontIturria.FitxaHasieraLangile()));
            tabBar.Items.Add(SortuLangileOrria<NireTxartelakOrria>("Nire txartelak", IkonoFontIturria.FitxaNireTxartelak()));
            tabBar.Items.Add(SortuEzarpenak());
        }

        shell.Items.Add(tabBar);
    }

    private ShellContent SortuAdministratzaileOrria<T>(string titulua, FontImageSource ikonoa) where T : Page
    {
        var orria = _zerbitzuHornitzailea.GetRequiredService<T>();
        return new ShellContent
        {
            Title = titulua,
            Content = orria,
            Route = typeof(T).Name,
            Icon = ikonoa
        };
    }

    private ShellContent SortuLangileOrria<T>(string titulua, FontImageSource ikonoa) where T : Page
    {
        var orria = _zerbitzuHornitzailea.GetRequiredService<T>();
        return new ShellContent
        {
            Title = titulua,
            Content = orria,
            Route = typeof(T).Name,
            Icon = ikonoa
        };
    }

    private ShellContent SortuEzarpenak()
    {
        var orria = _zerbitzuHornitzailea.GetRequiredService<EzarpenakOrria>();
        return new ShellContent
        {
            Title = "Ezarpenak",
            Content = orria,
            Route = nameof(EzarpenakOrria),
            Icon = IkonoFontIturria.FitxaEzarpenak()
        };
    }
}
