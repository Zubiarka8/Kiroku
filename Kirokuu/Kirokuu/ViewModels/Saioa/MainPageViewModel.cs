using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.Zerbitzuak;
using Microsoft.Extensions.Logging;

namespace Kirokuu.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    private readonly KredentzialEgiaztapenZerbitzua _kredentzialEgiaztapenZerbitzua;
    private readonly ILogger<MainPageViewModel> _logger;
    private readonly SemaphoreSlim _egiaztapenSarraila = new(1, 1);

    public MainPageViewModel(
        KredentzialEgiaztapenZerbitzua kredentzialEgiaztapenZerbitzua,
        ILogger<MainPageViewModel> logger)
    {
        _kredentzialEgiaztapenZerbitzua = kredentzialEgiaztapenZerbitzua
            ?? throw new ArgumentNullException(nameof(kredentzialEgiaztapenZerbitzua));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        EgoeraMezua = "Comprobando credenciales…";
    }

    [ObservableProperty]
    private bool _isKargatzean;

    [ObservableProperty]
    private string? _erroreMezua;

    [ObservableProperty]
    private string _egoeraMezua;

    [ObservableProperty]
    private string _xehetasunMezua = string.Empty;

    [RelayCommand]
    private async Task KargatuEgiaztapenaAsync()
    {
        if (!await _egiaztapenSarraila.WaitAsync(0).ConfigureAwait(false))
            return;

        try
        {
            IsKargatzean = true;
            ErroreMezua = null;
            XehetasunMezua = string.Empty;
            EgoeraMezua = "Comprobando credenciales…";

            var emaitza = await _kredentzialEgiaztapenZerbitzua.EgiaztatuAsync(CancellationToken.None).ConfigureAwait(true);

            switch (emaitza.Mota)
            {
                case KredentzialEgiaztapenMota.Ongi:
                    EgoeraMezua = "Las credenciales están bien";
                    XehetasunMezua = string.Empty;
                    break;
                case KredentzialEgiaztapenMota.Gaizki:
                    EgoeraMezua = "Las credenciales están mal";
                    XehetasunMezua = emaitza.Xehetasuna ?? string.Empty;
                    break;
                case KredentzialEgiaztapenMota.Errorea:
                    EgoeraMezua = "Ha habido un error";
                    XehetasunMezua = emaitza.Xehetasuna ?? string.Empty;
                    break;
            }
        }
        catch (HttpRequestException httpEx)
        {
            ErroreMezua = "Sare errorea: konexioa egiaztatu eta saiatu berriro.";
            EgoeraMezua = "Ha habido un error";
            XehetasunMezua = ErroreMezua;
            _logger.LogError(httpEx, "MainPage: sare errorea kredentzialak egiaztatzean.");
        }
        catch (TaskCanceledException tcEx)
        {
            ErroreMezua = "Eskaerak denbora muga gainditu du. Saiatu berriro.";
            EgoeraMezua = "Ha habido un error";
            XehetasunMezua = ErroreMezua;
            _logger.LogError(tcEx, "MainPage: denbora muga kredentzialak egiaztatzean.");
        }
        catch (UnauthorizedAccessException uaEx)
        {
            ErroreMezua = "Baimena ukatu da. Ezarpenetan baimena eman.";
            EgoeraMezua = "Ha habido un error";
            XehetasunMezua = ErroreMezua;
            _logger.LogError(uaEx, "MainPage: baimen errorea kredentzialak egiaztatzean.");
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            EgoeraMezua = "Ha habido un error";
            XehetasunMezua = ErroreMezua;
            _logger.LogError(ex, "MainPage: ustekabeko errorea kredentzialak egiaztatzean.");
        }
        finally
        {
            IsKargatzean = false;
            _egiaztapenSarraila.Release();
        }
    }
}
