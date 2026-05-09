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
    private void TxertaturikAroba() => Posta += "@";

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
        catch (InvalidOperationException opEx)
        {
            ErroreMezua = "Eragiketa baliogabea. Berriz saiatu saioa hasita.";
            _logger.LogError(opEx, "Saio hasiera: nabigazio errorea agertzean.");
        }
        catch (TursoExekuzioSalbuespena libEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                ?? "Datu-base errorea: ezin izan da kargatu. Saiatu berriro.";
            _logger.LogError(libEx, "Saio hasiera: Turso/libSQL errorea agertzean.");
        }
        catch (KeyNotFoundException knfEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ZutabeEskemaMezua;
            _logger.LogError(knfEx, "Saio hasiera: zutabe edo mapa errorea agertzean.");
        }
        catch (FormatException fmtEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.BalioFormatuMezua;
            _logger.LogError(fmtEx, "Saio hasiera: balio formatu errorea agertzean.");
        }
        catch (UnauthorizedAccessException uaaEx)
        {
            ErroreMezua = "Baimena ukatu da. Ezarpenetan baimena eman.";
            _logger.LogError(uaaEx, "Saio hasiera: baimena ukatua agertzean.");
        }
        catch (IOException ioEx)
        {
            ErroreMezua = "Fitxategi errorea: ezin izan dira saioaren datuak irakurri.";
            _logger.LogError(ioEx, "Saio hasiera: fitxategi errorea agertzean.");
        }
        catch (AggregateException aggEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.AgregatuarenBarnekoErabiltzaileMezua(aggEx)
                ?? "Datu-base edo sare errorea: saiatu berriro.";
            _logger.LogError(aggEx, "Saio hasiera: salbuespen agregatua agertzean.");
        }
        catch (Exception ex)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(ex)
                ?? "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
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
                case SaioHasieraEmaitzaMota.KontuaDesaktibatuta:
                    ErroreMezua = "Kontua desaktibatuta dago. Jarri harremanetan administratzailearekin.";
                    break;
                default:
                    ErroreMezua = "Ezin izan da saioa hasi. Saiatu berriro.";
                    break;
            }
        }
        catch (TursoExekuzioSalbuespena libEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                ?? "Datu-base errorea: ezin izan da saioa hasi. Saiatu berriro.";
            _logger.LogError(libEx, "Saio hasiera: Turso/libSQL errorea.");
        }
        catch (KeyNotFoundException knfEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ZutabeEskemaMezua;
            _logger.LogError(knfEx, "Saio hasiera: zutabe edo mapa errorea.");
        }
        catch (FormatException fmtEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.BalioFormatuMezua;
            _logger.LogError(fmtEx, "Saio hasiera: balio formatu errorea.");
        }
        catch (SQLiteException sqlEx)
        {
            ErroreMezua = "Datu-base errorea: ezin izan da saioa hasi. Saiatu berriro.";
            _logger.LogError(sqlEx, "Saio hasiera: SQLite errorea.");
        }
        catch (HttpRequestException httpEx)
        {
            ErroreMezua = "Sare errorea: konexioa egiaztatu eta saiatu berriro.";
            _logger.LogError(httpEx, "Saio hasiera: sare errorea (Turso?).");
        }
        catch (TaskCanceledException)
        {
            ErroreMezua = "Eskaerak denbora muga gainditu du. Saiatu berriro.";
        }
        catch (OperationCanceledException)
        {
            ErroreMezua = "Eskaerak denbora muga gainditu du edo eragiketa ezeztatu da. Saiatu berriro.";
        }
        catch (UnauthorizedAccessException uaaEx)
        {
            ErroreMezua = "Baimena ukatu da. Ezarpenetan baimena eman.";
            _logger.LogError(uaaEx, "Saio hasiera: baimena ukatua (SecureStorage edo sistema).");
        }
        catch (IOException ioEx)
        {
            ErroreMezua = "Fitxategi errorea: ezin izan dira saioaren datuak gorde.";
            _logger.LogError(ioEx, "Saio hasiera: fitxategi errorea (SecureStorage?).");
        }
        catch (InvalidOperationException opEx)
        {
            ErroreMezua = "Eragiketa baliogabea. Berriz saiatu saioa hasita.";
            _logger.LogError(opEx, "Saio hasiera: nabigazio errorea.");
        }
        catch (AggregateException aggEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.AgregatuarenBarnekoErabiltzaileMezua(aggEx)
                ?? "Datu-base edo sare errorea: saiatu berriro.";
            _logger.LogError(aggEx, "Saio hasiera: salbuespen agregatua.");
        }
        catch (Exception ex)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(ex)
                ?? "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
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
        catch (UnauthorizedAccessException uaaEx)
        {
            ErroreMezua = "Baimena ukatu da. Ezarpenetan baimena eman.";
            _logger.LogError(uaaEx, "Erregistro orrira joatean: baimena ukatua.");
        }
        catch (IOException ioEx)
        {
            ErroreMezua = "Fitxategi errorea: ezin izan da nabigazioa burutu.";
            _logger.LogError(ioEx, "Erregistro orrira joatean: fitxategi errorea.");
        }
        catch (AggregateException aggEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.AgregatuarenBarnekoErabiltzaileMezua(aggEx)
                ?? "Datu-base edo sare errorea: saiatu berriro.";
            _logger.LogError(aggEx, "Erregistro orrira joatean: salbuespen agregatua.");
        }
        catch (Exception ex)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(ex)
                ?? "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "Erregistro orrira joatean: ustekabeko errorea.");
        }
    }
}
