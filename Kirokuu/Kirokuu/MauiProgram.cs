using CommunityToolkit.Maui;
using Kirokuu.Pages;
using Kirokuu.ViewModels;
using Kirokuu.Zerbitzuak;
using Kirokuu.ZerbitzuakSaioa;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using SkiaSharp.Views.Maui.Controls.Hosting;
using SQLitePCL;

namespace Kirokuu;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        Batteries_V2.Init();

        var dotEnvKargatua = InguruneKargatzailea.KargatuDotEnv();
        if (!InguruneKargatzailea.TursoAldagaiNagusiakDaude())
        {
            var prozesuKarpeta = Environment.ProcessPath is { } pk ? Path.GetDirectoryName(pk) : null;
            if (!string.IsNullOrEmpty(prozesuKarpeta))
                dotEnvKargatua = InguruneKargatzailea.KargatuDotEnv(prozesuKarpeta) || dotEnvKargatua;
        }

        if (!InguruneKargatzailea.TursoAldagaiNagusiakDaude())
        {
            // AppContext.BaseDirectory: single-file/AOT seguruan dabil (Assembly.Location hutsik geratzen da → IL3000).
            var multzoKarpeta = AppContext.BaseDirectory;
            if (!string.IsNullOrEmpty(multzoKarpeta))
                dotEnvKargatua = InguruneKargatzailea.KargatuDotEnv(multzoKarpeta) || dotEnvKargatua;
        }
#if ANDROID
        if (!InguruneKargatzailea.TursoAldagaiNagusiakDaude())
        {
            var paketetikKargatua = InguruneTursoPaketekoAndroid.KargatuTursoGarapenIngurunea();
            dotEnvKargatua = paketetikKargatua || dotEnvKargatua;
        }
#endif
        #region agent log
        var envBideaOndoren = InguruneKargatzailea.BilatuEnvFitxategiarenBidea();
        var envGurasoOndoren = envBideaOndoren is null ? null : Path.GetDirectoryName(envBideaOndoren);
        InguruneKargatzailea.ErantsiAgenteDebugNeurria(envGurasoOndoren, "D", "MauiProgram.cs:CreateMauiApp:karga_ondoren", "maui_program_ingurunea", new Dictionary<string, object?>
        {
            ["dotEnvKargatuBool"] = dotEnvKargatua,
            ["tursoUrlDago"] = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TURSO_DATABASE_URL")),
            ["tursoTokenDago"] = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TURSO_AUTH_TOKEN")),
            ["cloudinaryIzenaDago"] = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME"))
        });
        #endregion
#if DEBUG
        if (!dotEnvKargatua)
        {
            using var inguruneLogiAlorra = LoggerFactory.Create(b =>
            {
                b.AddDebug();
                b.SetMinimumLevel(LogLevel.Debug);
            });
            inguruneLogiAlorra.CreateLogger(nameof(MauiProgram)).LogWarning(
                ".env fitxategia ez da aurkitu edo kargatu. TURSO_* aldagaiak hutsik badaude SQLite lokala erabiliko da.");
        }
#endif

        var builder = MauiApp.CreateBuilder();
        builder
            .UseSkiaSharp()
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .AddAudio()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
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
        builder.Services.AddTransient<AdministratzaileHasieraViewModel>();
        builder.Services.AddTransient<ErabiltzaileBerriaViewModel>();
        builder.Services.AddTransient<ErabiltzaileXehetasunViewModel>();
        builder.Services.AddTransient<NireTxartelakViewModel>();
        builder.Services.AddTransient<TxartelBerriaViewModel>();
        builder.Services.AddTransient<TxartelKanbanViewModel>();
        builder.Services.AddTransient<LangileaTxartelaXehetasunViewModel>();
        builder.Services.AddTransient<TxostenOnarpenXehetasunViewModel>();
        builder.Services.AddTransient<TxostenGuztiekViewModel>();
        builder.Services.AddTransient<AplikazioIrekitzeViewModel>();
        builder.Services.AddTransient<AplikazioIrekitzeOrria>();
        builder.Services.AddTransient<SaioHasieraOrria>();
        builder.Services.AddTransient<ErregistroOrria>();
        builder.Services.AddTransient<HasieraOrria>();
        builder.Services.AddTransient<NireTxartelakOrria>();
        builder.Services.AddTransient<TxartelBerriaOrria>();
        builder.Services.AddTransient<TxartelKanbanOrria>();
        builder.Services.AddTransient<LangileaTxartelaXehetasunOrria>();
        builder.Services.AddTransient<LasterEdukiaOrria>();
        builder.Services.AddTransient<EzarpenakOrria>();
        builder.Services.AddTransient<LangileZerrendaOrria>();
        builder.Services.AddTransient<AdministratzaileHasieraOrria>();
        builder.Services.AddTransient<ErabiltzaileBerriaOrria>();
        builder.Services.AddTransient<ErabiltzaileXehetasunOrria>();
        builder.Services.AddTransient<TxostenOnarpenXehetasunOrria>();
        builder.Services.AddTransient<TxostenGuztiekOrria>();
        builder.Services.AddTransient<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#if ANDROID
        builder.Logging.AddProvider(new LogcatLogatzaileHornitzailea());
#endif
        builder.Logging.SetMinimumLevel(LogLevel.Information);
#endif

        return builder.Build();
    }
}
