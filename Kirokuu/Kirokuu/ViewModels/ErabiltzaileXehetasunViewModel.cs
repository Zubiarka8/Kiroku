using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.Zerbitzuak;
using Kirokuu.ZerbitzuakSaioa;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using SQLite;
using System.Net.Http;

namespace Kirokuu.ViewModels;

[QueryProperty(nameof(ErabiltzaileIdQuery), "ErabiltzaileId")]
public partial class ErabiltzaileXehetasunViewModel : ObservableObject
{
    private readonly AutorizazioZerbitzua _autorizazioZerbitzua;
    private readonly ErabiltzaileZerbitzua _erabiltzaileZerbitzua;
    private readonly BerrespenLeihoZerbitzua _berrespenLeihoZerbitzua;
    private readonly ILogger<ErabiltzaileXehetasunViewModel> _logger;

    private int _erabiltzaileIdZenbakia;

    public ErabiltzaileXehetasunViewModel(
        AutorizazioZerbitzua autorizazioZerbitzua,
        ErabiltzaileZerbitzua erabiltzaileZerbitzua,
        BerrespenLeihoZerbitzua berrespenLeihoZerbitzua,
        ILogger<ErabiltzaileXehetasunViewModel> logger)
    {
        _autorizazioZerbitzua = autorizazioZerbitzua ?? throw new ArgumentNullException(nameof(autorizazioZerbitzua));
        _erabiltzaileZerbitzua = erabiltzaileZerbitzua ?? throw new ArgumentNullException(nameof(erabiltzaileZerbitzua));
        _berrespenLeihoZerbitzua = berrespenLeihoZerbitzua ?? throw new ArgumentNullException(nameof(berrespenLeihoZerbitzua));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [ObservableProperty]
    private string _erabiltzaileIdQuery = string.Empty;

    partial void OnErabiltzaileIdQueryChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        {
            _erabiltzaileIdZenbakia = 0;
            return;
        }

        _erabiltzaileIdZenbakia = id;
        _ = KargatuAsync();
    }

    [ObservableProperty]
    private bool _isKargatzean;

    [ObservableProperty]
    private string? _erroreMezua;

    [ObservableProperty]
    private string _izena = string.Empty;

    [ObservableProperty]
    private string _abizena = string.Empty;

    [ObservableProperty]
    private string _abizena2 = string.Empty;

    [ObservableProperty]
    private string _dni = string.Empty;

    [ObservableProperty]
    private string _posta = string.Empty;

    [ObservableProperty]
    private string _kargoa = string.Empty;

    [ObservableProperty]
    private bool _aktiboa;

    [ObservableProperty]
    private string _pasahitzaBerria = string.Empty;

    [ObservableProperty]
    private bool _pasahitzaBerriaMaskaratuta = true;

    [ObservableProperty]
    private string _pasahitzaBegiarenIzena = "begia_irekita";

    [ObservableProperty]
    private string _pasahitzaBegiarenDeskribapena = "Erakutsi pasahitza";

    partial void OnPasahitzaBerriaMaskaratutaChanged(bool value)
    {
        PasahitzaBegiarenIzena = value ? "begia_irekita" : "begia_itxita";
        PasahitzaBegiarenDeskribapena = value ? "Erakutsi pasahitza" : "Ezkutatu pasahitza";
    }

    [RelayCommand]
    private void AlderantzikatuPasahitzaMaska() => PasahitzaBerriaMaskaratuta = !PasahitzaBerriaMaskaratuta;

    private async Task KargatuAsync()
    {
        ErroreMezua = null;
        if (_erabiltzaileIdZenbakia <= 0)
            return;

        try
        {
            IsKargatzean = true;
            if (!await _autorizazioZerbitzua.DaAdministratzaileaAsync().ConfigureAwait(true))
            {
                ErroreMezua = "Ez duzu baimenik.";
                return;
            }

            var erabiltzailea = await _erabiltzaileZerbitzua.EskuratuErabiltzaileaIdzAsync(_erabiltzaileIdZenbakia).ConfigureAwait(true);
            if (erabiltzailea is null)
            {
                ErroreMezua = "Erabiltzailea ez da aurkitu.";
                return;
            }

            if (erabiltzailea.Rola != (int)ErabiltzaileRola.Langilea)
            {
                ErroreMezua = "Administratzaile profilak ezin dira hemen editatu.";
                return;
            }

            Izena = erabiltzailea.Izena;
            Abizena = erabiltzailea.Abizena;
            Abizena2 = erabiltzailea.Abizena2;
            Dni = erabiltzailea.DNI;
            Posta = erabiltzailea.Posta;
            Kargoa = erabiltzailea.Kargoa;
            Aktiboa = erabiltzailea.Aktiboa != 0;
            PasahitzaBerria = string.Empty;
        }
        catch (TursoExekuzioSalbuespena libEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                ?? "Datu-base errorea: ezin izan da irakurri. Saiatu berriro.";
            _logger.LogError(libEx, "Erabiltzaile xehetasuna: Turso errorea.");
        }
        catch (KeyNotFoundException knfEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ZutabeEskemaMezua;
            _logger.LogError(knfEx, "Erabiltzaile xehetasuna: mapa errorea.");
        }
        catch (FormatException fmtEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.BalioFormatuMezua;
            _logger.LogError(fmtEx, "Erabiltzaile xehetasuna: formatu errorea.");
        }
        catch (SQLiteException sqlEx)
        {
            ErroreMezua = "Datu-base errorea: ezin izan da irakurri. Saiatu berriro.";
            _logger.LogError(sqlEx, "Erabiltzaile xehetasuna: SQLite errorea.");
        }
        catch (HttpRequestException httpEx)
        {
            ErroreMezua = "Sare errorea: konexioa egiaztatu eta saiatu berriro.";
            _logger.LogError(httpEx, "Erabiltzaile xehetasuna: sare errorea.");
        }
        catch (InvalidOperationException opEx)
        {
            ErroreMezua = "Eragiketa baliogabea. Berriz saiatu saioa hasita.";
            _logger.LogError(opEx, "Erabiltzaile xehetasuna: eragiketa baliogabea.");
        }
        catch (TaskCanceledException)
        {
            ErroreMezua = "Eskaerak denbora muga gainditu du. Saiatu berriro.";
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "Erabiltzaile xehetasuna: ustekabeko errorea.");
        }
        finally
        {
            IsKargatzean = false;
        }
    }

    [RelayCommand]
    private async Task GordeAsync()
    {
        ErroreMezua = null;
        if (_erabiltzaileIdZenbakia <= 0)
            return;

        if (!await _autorizazioZerbitzua.DaAdministratzaileaAsync().ConfigureAwait(true))
        {
            ErroreMezua = "Ez duzu baimenik.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Izena) || string.IsNullOrWhiteSpace(Abizena) ||
            string.IsNullOrWhiteSpace(Posta))
        {
            ErroreMezua = "Izena, abizena eta posta beharrezkoak dira.";
            return;
        }

        try
        {
            IsKargatzean = true;
            await _erabiltzaileZerbitzua.AdministratzaileakEguneratuErabiltzaileProfilaAsync(
                    _erabiltzaileIdZenbakia,
                    Izena,
                    Abizena,
                    Abizena2,
                    Dni,
                    Posta,
                    Kargoa,
                    Aktiboa ? 1 : 0)
                .ConfigureAwait(true);

            if (!string.IsNullOrWhiteSpace(PasahitzaBerria))
            {
                if (PasahitzaBerria.Trim().Length < 8)
                {
                    ErroreMezua = "Pasahitz berriak gutxienez 8 karaktere izan behar ditu.";
                    return;
                }

                await _erabiltzaileZerbitzua.AdministratzaileakBerrezarriPasahitzaLangilearentzatAsync(
                        _erabiltzaileIdZenbakia,
                        PasahitzaBerria)
                    .ConfigureAwait(true);
            }

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await BokadilloErakustzailea.SaiatuErakutsiAsync("Aldaketak gorde dira.", _logger).ConfigureAwait(true);
            }).ConfigureAwait(true);

            await Shell.Current.GoToAsync("..").ConfigureAwait(true);
        }
        catch (SQLiteException sqlEx) when (sqlEx.Result == SQLite3.Result.Constraint)
        {
            ErroreMezua = "Datu bikoiztua: posta edo DNI jadanik erabilita.";
            _logger.LogWarning(sqlEx, "Erabiltzaile xehetasuna: murrizketa.");
        }
        catch (SQLiteException sqlEx)
        {
            ErroreMezua = "Datu-base errorea: ezin izan da gorde. Saiatu berriro.";
            _logger.LogError(sqlEx, "Erabiltzaile xehetasuna: SQLite errorea.");
        }
        catch (HttpRequestException httpEx)
        {
            ErroreMezua = "Sare errorea: konexioa egiaztatu eta saiatu berriro.";
            _logger.LogError(httpEx, "Erabiltzaile xehetasuna: sare errorea.");
        }
        catch (InvalidOperationException opEx)
        {
            ErroreMezua = "Eragiketa baliogabea. Berriz saiatu.";
            _logger.LogError(opEx, "Erabiltzaile xehetasuna: eragiketa baliogabea.");
        }
        catch (ArgumentException argEx)
        {
            ErroreMezua = "Pasahitz berria baliogabea da.";
            _logger.LogError(argEx, "Erabiltzaile xehetasuna: argumentu errorea.");
        }
        catch (TaskCanceledException)
        {
            ErroreMezua = "Eskaerak denbora muga gainditu du. Saiatu berriro.";
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "Erabiltzaile xehetasuna: gorde errorea.");
        }
        finally
        {
            IsKargatzean = false;
        }
    }

    [RelayCommand]
    private async Task DesaktibatuAsync()
    {
        ErroreMezua = null;
        if (_erabiltzaileIdZenbakia <= 0)
            return;

        var baieztatu = await _berrespenLeihoZerbitzua.BerretsiAsync(
                "Baieztatu",
                "Ziur zaude erabiltzaile hau desaktibatu nahi duzula?")
            .ConfigureAwait(true);

        if (!baieztatu)
            return;

        Aktiboa = false;
        await GordeAsync().ConfigureAwait(true);
    }
}
