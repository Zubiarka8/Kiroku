using CommunityToolkit.Mvvm.Input;
using Kirokuu.ZerbitzuakSaioa;

namespace Kirokuu.ViewModels;

public partial class EzarpenakViewModel
{
    [RelayCommand]
    private async Task SaioaItxiAsync()
    {
        ErroreMezua = null;
        try
        {
            var berretsi = await _berrespenLeihoZerbitzua.BerretsiAsync(
                "Saioa itxi",
                "Ziur zaude saioa itxi nahi duzula?").ConfigureAwait(true);

            if (!berretsi)
                return;

            await _saioaGordetzeZerbitzua.GarbituAsync().ConfigureAwait(true);
            await _nabigazioNagusia.JoanSaioHasieraraAsync().ConfigureAwait(true);
            await BokadilloErakustzailea.SaiatuErakutsiAsync("Saioa itxita.", _logger).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            ViewModelSalbuespenTratatzailea.TratatuIrakurketa(ex, m => ErroreMezua = m, _logger, "Ezarpenak saioa itxi");
        }
    }
}
