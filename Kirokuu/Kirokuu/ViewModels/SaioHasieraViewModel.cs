using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.Pages;
using Kirokuu.Zerbitzuak;
using Kirokuu.ZerbitzuakSaioa;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using SQLite;
using System.IO;
using System.Net.Http;

namespace Kirokuu.ViewModels;

public partial class SaioHasieraViewModel : ObservableObject
{
    private readonly ErabiltzaileZerbitzua _erabiltzaileZerbitzua;
    private readonly SaioaGordetzeZerbitzua _saioaGordetzeZerbitzua;
    private readonly INabigazioNagusia _nabigazioNagusia;
    private readonly IServiceProvider _zerbitzuHornitzailea;
    private readonly ILogger<SaioHasieraViewModel> _logger;

    private CancellationTokenSource? _blokeoKontadoraKts;

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

    [ObservableProperty]
    private bool _kontuaBlokeatuta;

    [ObservableProperty]
    private string _blokeoKontadorea = string.Empty;

    [ObservableProperty]
    private bool _blokeoKontadoraIkagarri;

    [ObservableProperty]
    private bool _pasahitzaMaskaratuta = true;

    [ObservableProperty]
    private string _pasahitzaBegiarenIrudiarenIzena = "begia_irekita";

    [ObservableProperty]
    private string _pasahitzaBegiarenDeskribapena = "Erakutsi pasahitza";

    partial void OnPasahitzaMaskaratutaChanged(bool value)
    {
        PasahitzaBegiarenIrudiarenIzena = value ? "begia_irekita" : "begia_itxita";
        PasahitzaBegiarenDeskribapena = value ? "Erakutsi pasahitza" : "Ezkutatu pasahitza";
    }

    [RelayCommand]
    private void AlderantzikatuPasahitzaMaska() => PasahitzaMaskaratuta = !PasahitzaMaskaratuta;

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
        catch (Exception ex)
        {
            ViewModelSalbuespenTratatzailea.TratatuIrakurketa(ex, m => ErroreMezua = m, _logger, "Saio hasiera agertzean");
        }
        finally
        {
            IsKargatzean = false;
        }
    }

    [RelayCommand]
    private async Task SaioaHasiAsync()
    {
        if (KontuaBlokeatuta)
            return;

        ErroreMezua = null;
        if (string.IsNullOrWhiteSpace(Posta) || string.IsNullOrWhiteSpace(Pasahitza))
        {
            ErroreMezua = "Posta eta pasahitza bete behar dituzu.";
            return;
        }

        try
        {
            IsKargatzean = true;
            var (mota, erabiltzailea, blokeoaGeratzen) = await _erabiltzaileZerbitzua.SaioaHasiAsync(Posta, Pasahitza).ConfigureAwait(true);
            switch (mota)
            {
                case SaioHasieraEmaitzaMota.Ongi when erabiltzailea is not null:
                    await _saioaGordetzeZerbitzua.GordeAsync(erabiltzailea).ConfigureAwait(true);
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await BokadilloErakustzailea.SaiatuErakutsiAsync("Saioa ondo hasi da.", _logger)
                            .ConfigureAwait(true);
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
                case SaioHasieraEmaitzaMota.SaioDenborazBlokeatuta:
                    {
                        var geratzen = blokeoaGeratzen ?? TimeSpan.FromMinutes(ErabiltzaileZerbitzua.SaioBlokeoaIraupenaMinutuak);
                        var minutuak = Math.Max(1, (int)Math.Ceiling(geratzen.TotalMinutes));
                        ErroreMezua = $"Saio askotan okerrak direla eta, kontua blokeatu egin da {minutuak} minutu arte.";
                        KontuaBlokeatuta = true;
                        BlokeoKontadoraIkagarri = true;
                        _blokeoKontadoraKts?.Cancel();
                        _blokeoKontadoraKts = new CancellationTokenSource();
                        _ = KontadoraJarriAsync(geratzen, _blokeoKontadoraKts.Token);
                        break;
                    }
                case SaioHasieraEmaitzaMota.KontuaDesaktibatuta:
                    ErroreMezua = "Kontua desaktibatuta dago. Jarri harremanetan administratzailearekin.";
                    break;
                default:
                    ErroreMezua = "Ezin izan da saioa hasi. Saiatu berriro.";
                    break;
            }
        }
        catch (Exception ex)
        {
            ViewModelSalbuespenTratatzailea.TratatuIrakurketa(ex, m => ErroreMezua = m, _logger, "Saio hasiera");
        }
        finally
        {
            IsKargatzean = false;
        }
    }

    private async Task KontadoraJarriAsync(TimeSpan geratzen, CancellationToken ezeztapena)
    {
        try
        {
            var amaieraUtc = DateTime.UtcNow + geratzen;
            while (!ezeztapena.IsCancellationRequested)
            {
                var oraindikGeratzen = amaieraUtc - DateTime.UtcNow;
                if (oraindikGeratzen <= TimeSpan.Zero)
                    break;

                var min = (int)oraindikGeratzen.TotalMinutes;
                var sek = oraindikGeratzen.Seconds;
                BlokeoKontadorea = $"{min}:{sek:D2}";

                await Task.Delay(1000, ezeztapena).ConfigureAwait(true);
            }
        }
        catch (TaskCanceledException) { }
        catch (OperationCanceledException) { }

        if (!ezeztapena.IsCancellationRequested)
        {
            KontuaBlokeatuta = false;
            BlokeoKontadoraIkagarri = false;
            BlokeoKontadorea = string.Empty;
            ErroreMezua = null;
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
        catch (Exception ex)
        {
            ViewModelSalbuespenTratatzailea.TratatuIrakurketa(ex, m => ErroreMezua = m, _logger, "Erregistro orrira");
        }
    }
}
