using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.Pages;
using Kirokuu.ZerbitzuakSaioa;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using SQLite;

namespace Kirokuu.ViewModels;

public partial class SaioHasieraViewModel : ObservableObject
{
    private readonly ErabiltzaileZerbitzua _erabiltzaileZerbitzua;
    private readonly SaioaGordetzeZerbitzua _saioaGordetzeZerbitzua;
    private readonly INabigazioNagusia _nabigazioNagusia;
    private readonly IServiceProvider _zerbitzuHornitzailea;
    private readonly ILogger<SaioHasieraViewModel> _logger;

    public SaioHasieraViewModel(
        ErabiltzaileZerbitzua erabiltzaileZerbitzua,
        SaioaGordetzeZerbitzua saioaGordetzeZerbitzua,
        INabigazioNagusia nabigazioNagusia,
        IServiceProvider zerbitzuHornitzailea,
        ILogger<SaioHasieraViewModel> logger)
    {
        _erabiltzaileZerbitzua = erabiltzaileZerbitzua ?? throw new ArgumentNullException(nameof(erabiltzaileZerbitzua));
        _saioaGordetzeZerbitzua = saioaGordetzeZerbitzua ?? throw new ArgumentNullException(nameof(saioaGordetzeZerbitzua));
        _nabigazioNagusia = nabigazioNagusia ?? throw new ArgumentNullException(nameof(nabigazioNagusia));
        _zerbitzuHornitzailea = zerbitzuHornitzailea ?? throw new ArgumentNullException(nameof(zerbitzuHornitzailea));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [ObservableProperty]
    private string _posta = string.Empty;

    [ObservableProperty]
    private string _pasahitza = string.Empty;

    [ObservableProperty]
    private bool _isKargatzean;

    [ObservableProperty]
    private string? _erroreMezua;

    [RelayCommand]
    private async Task AgertzenDeneanAsync()
    {
        ErroreMezua = null;
        try
        {
            IsKargatzean = true;
            if (await _saioaGordetzeZerbitzua.BadagoSaioaAsync().ConfigureAwait(true))
                await _nabigazioNagusia.JoanAppShelleraAsync().ConfigureAwait(true);
        }
        catch (InvalidOperationException opEx)
        {
            ErroreMezua = "Eragiketa baliogabea. Berriz saiatu saioa hasita.";
            _logger.LogError(opEx, "Saio hasiera: nabigazio errorea agertzean.");
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "Saio hasiera: ustekabeko errorea agertzean.");
        }
        finally
        {
            IsKargatzean = false;
        }
    }

    [RelayCommand]
    private async Task SaioaHasiAsync()
    {
        ErroreMezua = null;
        if (string.IsNullOrWhiteSpace(Posta) || string.IsNullOrWhiteSpace(Pasahitza))
        {
            ErroreMezua = "Posta eta pasahitza bete behar dituzu.";
            return;
        }

        try
        {
            IsKargatzean = true;
            var (mota, erabiltzailea) = await _erabiltzaileZerbitzua.SaioaHasiAsync(Posta, Pasahitza).ConfigureAwait(true);
            switch (mota)
            {
                case SaioHasieraEmaitzaMota.Ongi when erabiltzailea is not null:
                    await _saioaGordetzeZerbitzua.GordeAsync(erabiltzailea).ConfigureAwait(true);
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await Toast.Make("Saioa ondo hasi da.").Show().ConfigureAwait(true);
                    }).ConfigureAwait(true);
                    await _nabigazioNagusia.JoanAppShelleraAsync().ConfigureAwait(true);
                    break;
                case SaioHasieraEmaitzaMota.EzDaExistitzen:
                    ErroreMezua = "Posta hau ez dago erregistratuta.";
                    break;
                case SaioHasieraEmaitzaMota.PasahitzaOkerra:
                    ErroreMezua = "Pasahitza okerra da.";
                    break;
                case SaioHasieraEmaitzaMota.KontuaBlokeatuta:
                    ErroreMezua = "Kontua blokeatuta dago. Jarri harremanetan administratzailearekin.";
                    break;
                default:
                    ErroreMezua = "Ezin izan da saioa hasi. Saiatu berriro.";
                    break;
            }
        }
        catch (SQLiteException sqlEx)
        {
            ErroreMezua = "Datu-base errorea: ezin izan da saioa hasi. Saiatu berriro.";
            _logger.LogError(sqlEx, "Saio hasiera: SQLite errorea.");
        }
        catch (InvalidOperationException opEx)
        {
            ErroreMezua = "Eragiketa baliogabea. Berriz saiatu saioa hasita.";
            _logger.LogError(opEx, "Saio hasiera: nabigazio errorea.");
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "Saio hasiera: ustekabeko errorea.");
        }
        finally
        {
            IsKargatzean = false;
        }
    }

    [RelayCommand]
    private async Task JoanErregistoraAsync()
    {
        ErroreMezua = null;
        try
        {
            var orria = _zerbitzuHornitzailea.GetRequiredService<ErregistroOrria>();
            if (Application.Current?.Windows.FirstOrDefault()?.Page is NavigationPage nabigazioa)
                await nabigazioa.PushAsync(orria).ConfigureAwait(true);
            else
                throw new InvalidOperationException("Nabigazio orria ez da aurkitu.");
        }
        catch (InvalidOperationException opEx)
        {
            ErroreMezua = "Eragiketa baliogabea. Berriz saiatu saioa hasita.";
            _logger.LogError(opEx, "Erregistro orrira joatean.");
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "Erregistro orrira joatean: ustekabeko errorea.");
        }
    }
}
