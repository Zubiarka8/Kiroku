using Kirokuu.Pages;

namespace Kirokuu;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(TxartelBerriaOrria), typeof(TxartelBerriaOrria));
        Routing.RegisterRoute(nameof(ErabiltzaileBerriaOrria), typeof(ErabiltzaileBerriaOrria));
        Routing.RegisterRoute(nameof(ErabiltzaileXehetasunOrria), typeof(ErabiltzaileXehetasunOrria));
        Routing.RegisterRoute(nameof(TxostenOnarpenXehetasunOrria), typeof(TxostenOnarpenXehetasunOrria));
        Routing.RegisterRoute(nameof(LangileaTxartelaXehetasunOrria), typeof(LangileaTxartelaXehetasunOrria));
    }
}
