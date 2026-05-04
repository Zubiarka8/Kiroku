using CommunityToolkit.Maui;
using Kirokuu.Pages;
using Kirokuu.ViewModels;
using Kirokuu.Zerbitzuak;
using Kirokuu.ZerbitzuakSaioa;
using Microsoft.Extensions.Logging;

namespace Kirokuu;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        InguruneKargatzailea.KargatuDotEnv();

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<ArgazkiIgotzeZerbitzua>(_ => new ArgazkiIgotzeZerbitzua());
        builder.Services.AddSingleton<KredentzialEgiaztapenZerbitzua>();
        builder.Services.AddSingleton<PasahitzaZerbitzua>();
        builder.Services.AddSingleton<DatuBaseaZerbitzua>();
        builder.Services.AddSingleton<ErabiltzaileZerbitzua>();
        builder.Services.AddSingleton<SaioaGordetzeZerbitzua>();
        builder.Services.AddSingleton<AutorizazioZerbitzua>();
        builder.Services.AddSingleton<BerrespenLeihoZerbitzua>();
        builder.Services.AddSingleton<ShellFitxaEraikitzailea>();
        builder.Services.AddSingleton<INabigazioNagusia, NabigazioNagusia>();

        builder.Services.AddTransient<MainPageViewModel>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<SaioHasieraViewModel>();
        builder.Services.AddTransient<ErregistroViewModel>();
        builder.Services.AddTransient<HasieraViewModel>();
        builder.Services.AddTransient<EzarpenakViewModel>();
        builder.Services.AddTransient<LangileZerrendaViewModel>();
        builder.Services.AddTransient<SaioHasieraOrria>();
        builder.Services.AddTransient<ErregistroOrria>();
        builder.Services.AddTransient<HasieraOrria>();
        builder.Services.AddTransient<LasterEdukiaOrria>();
        builder.Services.AddTransient<EzarpenakOrria>();
        builder.Services.AddTransient<LangileZerrendaOrria>();
        builder.Services.AddTransient<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
