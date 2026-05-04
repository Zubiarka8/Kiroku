using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.ZerbitzuakSaioa;
using Libsql.Client;
using Microsoft.Extensions.Logging;
using SQLite;
using System.Net.Http;

namespace Kirokuu.ViewModels;

public partial class EzarpenakViewModel : ObservableObject
{
    private readonly AutorizazioZerbitzua _autorizazioZerbitzua;
    private readonly ErabiltzaileZerbitzua _erabiltzaileZerbitzua;
    private readonly SaioaGordetzeZerbitzua _saioaGordetzeZerbitzua;
    private readonly INabigazioNagusia _nabigazioNagusia;
    private readonly BerrespenLeihoZerbitzua _berrespenLeihoZerbitzua;
    private readonly ILogger<EzarpenakViewModel> _logger;

    public EzarpenakViewModel(
        AutorizazioZerbitzua autorizazioZerbitzua,
        ErabiltzaileZerbitzua erabiltzaileZerbitzua,
        SaioaGordetzeZerbitzua saioaGordetzeZerbitzua,
        INabigazioNagusia nabigazioNagusia,
        BerrespenLeihoZerbitzua berrespenLeihoZerbitzua,
        ILogger<EzarpenakViewModel> logger)
    {
        _autorizazioZerbitzua = autorizazioZerbitzua ?? throw new ArgumentNullException(nameof(autorizazioZerbitzua));
        _erabiltzaileZerbitzua = erabiltzaileZerbitzua ?? throw new ArgumentNullException(nameof(erabiltzaileZerbitzua));
        _saioaGordetzeZerbitzua = saioaGordetzeZerbitzua ?? throw new ArgumentNullException(nameof(saioaGordetzeZerbitzua));
        _nabigazioNagusia = nabigazioNagusia ?? throw new ArgumentNullException(nameof(nabigazioNagusia));
        _berrespenLeihoZerbitzua = berrespenLeihoZerbitzua ?? throw new ArgumentNullException(nameof(berrespenLeihoZerbitzua));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
    private string _posta = string.Empty;

    [RelayCommand]
    private async Task AgertzenDeneanAsync()
    {
        ErroreMezua = null;
        try
        {
            IsKargatzean = true;
            var id = await _autorizazioZerbitzua.EskuratuOraingoErabiltzaileIdAsync().ConfigureAwait(true);
            if (id is null)
            {
                ErroreMezua = "Saioa ez da aurkitu. Hasi saioa berriro.";
                return;
            }

            var profila = await _erabiltzaileZerbitzua.EskuratuProfilLaburpenaIdzAsync(id.Value).ConfigureAwait(true);
            if (profila is null)
            {
                ErroreMezua = "Erabiltzailearen datuak ez dira aurkitu.";
                return;
            }

            Izena = profila.Izena;
            Abizena = profila.Abizena;
            Posta = profila.Posta;
        }
        catch (LibsqlException libEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                ?? "Datu-base errorea: ezin izan da irakurri. Saiatu berriro.";
            _logger.LogError(libEx, "Ezarpenak: Turso/libSQL errorea.");
        }
        catch (KeyNotFoundException knfEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ZutabeEskemaMezua;
            _logger.LogError(knfEx, "Ezarpenak: zutabe edo mapa errorea.");
        }
        catch (FormatException fmtEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.BalioFormatuMezua;
            _logger.LogError(fmtEx, "Ezarpenak: balio formatu errorea.");
        }
        catch (SQLiteException sqlEx)
        {
            ErroreMezua = "Datu-base errorea: ezin izan da irakurri. Saiatu berriro.";
            _logger.LogError(sqlEx, "Ezarpenak: SQLite errorea.");
        }
        catch (HttpRequestException httpEx)
        {
            ErroreMezua = "Sare errorea: konexioa egiaztatu eta saiatu berriro.";
            _logger.LogError(httpEx, "Ezarpenak: sare errorea (Turso?).");
        }
        catch (InvalidOperationException opEx)
        {
            ErroreMezua = "Eragiketa baliogabea. Berriz saiatu saioa hasita.";
            _logger.LogError(opEx, "Ezarpenak: eragiketa baliogabea.");
        }
        catch (TaskCanceledException)
        {
            ErroreMezua = "Eskaerak denbora muga gainditu du. Saiatu berriro.";
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "Ezarpenak: ustekabeko errorea.");
        }
        finally
        {
            IsKargatzean = false;
        }
    }

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
            await Toast.Make("Saioa itxita.").Show().ConfigureAwait(true);
        }
        catch (InvalidOperationException opEx)
        {
            ErroreMezua = "Eragiketa baliogabea. Berriz saiatu saioa hasita.";
            _logger.LogError(opEx, "Ezarpenak: saioa ixtean.");
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "Ezarpenak: saioa ixtean ustekabekoa.");
        }
    }
}
