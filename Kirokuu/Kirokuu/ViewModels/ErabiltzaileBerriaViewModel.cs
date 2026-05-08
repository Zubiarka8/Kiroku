using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.Zerbitzuak;
using Kirokuu.ZerbitzuakSaioa;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using SQLite;
using System.Net.Http;

namespace Kirokuu.ViewModels;

public partial class ErabiltzaileBerriaViewModel : ObservableObject
{
    private readonly AutorizazioZerbitzua _autorizazioZerbitzua;
    private readonly ErabiltzaileZerbitzua _erabiltzaileZerbitzua;
    private readonly ILogger<ErabiltzaileBerriaViewModel> _logger;

    public ErabiltzaileBerriaViewModel(
        AutorizazioZerbitzua autorizazioZerbitzua,
        ErabiltzaileZerbitzua erabiltzaileZerbitzua,
        ILogger<ErabiltzaileBerriaViewModel> logger)
    {
        _autorizazioZerbitzua = autorizazioZerbitzua ?? throw new ArgumentNullException(nameof(autorizazioZerbitzua));
        _erabiltzaileZerbitzua = erabiltzaileZerbitzua ?? throw new ArgumentNullException(nameof(erabiltzaileZerbitzua));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [ObservableProperty] private string _izena = string.Empty;

    [ObservableProperty] private string _abizena = string.Empty;

    [ObservableProperty] private string _abizena2 = string.Empty;

    [ObservableProperty] private string _dni = string.Empty;

    [ObservableProperty] private string _kargoa = string.Empty;

    [ObservableProperty] private string _posta = string.Empty;

    [ObservableProperty] private string _pasahitza = string.Empty;

    [ObservableProperty] private string _pasahitzaBerretsi = string.Empty;

    [ObservableProperty] private bool _isKargatzean;

    [ObservableProperty] private string? _erroreMezua;

    [ObservableProperty] private bool _pasahitzaMaskaratuta = true;

    [ObservableProperty] private string _pasahitzaBegiarenIrudiarenIzena = "begia_irekita";

    [ObservableProperty] private string _pasahitzaBegiarenDeskribapena = "Erakutsi pasahitza";

    [ObservableProperty] private bool _pasahitzaBerretsiMaskaratuta = true;

    [ObservableProperty] private string _pasahitzaBerretsiBegiarenIzena = "begia_irekita";

    [ObservableProperty] private string _pasahitzaBerretsiBegiarenDeskribapena = "Erakutsi pasahitza";

    partial void OnPasahitzaMaskaratutaChanged(bool value)
    {
        PasahitzaBegiarenIrudiarenIzena = value ? "begia_irekita" : "begia_itxita";
        PasahitzaBegiarenDeskribapena = value ? "Erakutsi pasahitza" : "Ezkutatu pasahitza";
    }

    partial void OnPasahitzaBerretsiMaskaratutaChanged(bool value)
    {
        PasahitzaBerretsiBegiarenIzena = value ? "begia_irekita" : "begia_itxita";
        PasahitzaBerretsiBegiarenDeskribapena = value ? "Erakutsi pasahitza berretsia" : "Ezkutatu pasahitza berretsia";
    }

    [RelayCommand]
    private void AlderantzikatuPasahitzaMaska() => PasahitzaMaskaratuta = !PasahitzaMaskaratuta;

    [RelayCommand]
    private void AlderantzikatuPasahitzaBerretsiMaska() => PasahitzaBerretsiMaskaratuta = !PasahitzaBerretsiMaskaratuta;

    [RelayCommand]
    private async Task GordeAsync()
    {
        ErroreMezua = null;
        if (!await _autorizazioZerbitzua.DaAdministratzaileaAsync().ConfigureAwait(true))
        {
            ErroreMezua = "Ez duzu baimenik.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Izena) || string.IsNullOrWhiteSpace(Abizena) ||
            string.IsNullOrWhiteSpace(Abizena2) || string.IsNullOrWhiteSpace(Dni) ||
            string.IsNullOrWhiteSpace(Kargoa) || string.IsNullOrWhiteSpace(Posta) ||
            string.IsNullOrWhiteSpace(Pasahitza) || string.IsNullOrWhiteSpace(PasahitzaBerretsi))
        {
            ErroreMezua = "Eremu bat edo gehiago hutsik daude. Bete beharrezko eremuak.";
            return;
        }

        if (!Posta.Contains('@', StringComparison.Ordinal))
        {
            ErroreMezua = "Posta baliogabea da.";
            return;
        }

        if (!string.Equals(Pasahitza, PasahitzaBerretsi, StringComparison.Ordinal))
        {
            ErroreMezua = "Pasahitzak ez datoz bat.";
            return;
        }

        if (Pasahitza.Trim().Length < 8)
        {
            ErroreMezua = "Pasahitzak gutxienez 8 karaktere izan behar ditu.";
            return;
        }

        try
        {
            IsKargatzean = true;
            await _erabiltzaileZerbitzua.ErregistratuLangileaAsync(
                    Izena,
                    Abizena,
                    Abizena2,
                    Dni,
                    Kargoa,
                    Posta,
                    Pasahitza)
                .ConfigureAwait(true);

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await BokadilloErakustzailea.SaiatuErakutsiAsync("Langilea ondo sortu da.", _logger).ConfigureAwait(true);
            }).ConfigureAwait(true);

            await Shell.Current.GoToAsync("..").ConfigureAwait(true);
        }
        catch (ErabiltzaileMurrizketaSalbuespena murEx)
        {
            ErroreMezua = "Posta edo DNI bikoiztua.";
            _logger.LogWarning(murEx, "Erabiltzaile berria: murrizketa.");
        }
        catch (SQLiteException sqlEx) when (sqlEx.Result == SQLite3.Result.Constraint)
        {
            ErroreMezua = "Datu bikoiztua: erabiltzaile hau dagoeneko badago.";
            _logger.LogWarning(sqlEx, "Erabiltzaile berria: SQLite murrizketa.");
        }
        catch (SQLiteException sqlEx)
        {
            ErroreMezua = "Datu-base errorea: ezin izan da gorde. Saiatu berriro.";
            _logger.LogError(sqlEx, "Erabiltzaile berria: SQLite errorea.");
        }
        catch (HttpRequestException httpEx)
        {
            ErroreMezua = "Sare errorea: konexioa egiaztatu eta saiatu berriro.";
            _logger.LogError(httpEx, "Erabiltzaile berria: sare errorea.");
        }
        catch (InvalidOperationException opEx)
        {
            ErroreMezua = "Eragiketa baliogabea. Berriz saiatu saioa hasita.";
            _logger.LogError(opEx, "Erabiltzaile berria: eragiketa baliogabea.");
        }
        catch (TaskCanceledException)
        {
            ErroreMezua = "Eskaerak denbora muga gainditu du. Saiatu berriro.";
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "Erabiltzaile berria: ustekabeko errorea.");
        }
        finally
        {
            IsKargatzean = false;
        }
    }
}
