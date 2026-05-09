using Kirokuu.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace Kirokuu;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        JakinarazpenMauiZerbitzuErreferentzia.ZerbitzuHornitzailea = services;
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var aplikazioIrekitze = _services.GetRequiredService<AplikazioIrekitzeOrria>();
        return new Window(new NavigationPage(aplikazioIrekitze));
    }
}
