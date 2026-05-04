using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.Zerbitzuak;

namespace Kirokuu.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    private readonly KredentzialEgiaztapenZerbitzua _kredentzialEgiaztapenZerbitzua;
    private readonly SemaphoreSlim _egiaztapenSarraila = new(1, 1);

    public MainPageViewModel(KredentzialEgiaztapenZerbitzua kredentzialEgiaztapenZerbitzua)
    {
        _kredentzialEgiaztapenZerbitzua = kredentzialEgiaztapenZerbitzua
            ?? throw new ArgumentNullException(nameof(kredentzialEgiaztapenZerbitzua));
        EgoeraMezua = "Comprobando credenciales…";
    }

    [ObservableProperty]
    private bool _isKargatzean;

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
        finally
        {
            IsKargatzean = false;
            _egiaztapenSarraila.Release();
        }
    }
}
