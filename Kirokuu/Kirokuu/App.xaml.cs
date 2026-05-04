using Kirokuu.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace Kirokuu;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var saioHasiera = _services.GetRequiredService<SaioHasieraOrria>();
        return new Window(new NavigationPage(saioHasiera));
    }
}
