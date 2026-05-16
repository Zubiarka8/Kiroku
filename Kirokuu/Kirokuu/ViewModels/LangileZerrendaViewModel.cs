using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.DatuEreduak;
using Kirokuu.Pages;
using Kirokuu.Zerbitzuak;
using Kirokuu.ZerbitzuakSaioa;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using SQLite;
using System.Globalization;
using System.Net.Http;

namespace Kirokuu.ViewModels;

public partial class LangileZerrendaViewModel : ObservableObject
{
    private readonly AutorizazioZerbitzua _autorizazioZerbitzua;
    private readonly ErabiltzaileZerbitzua _erabiltzaileZerbitzua;
    private readonly ILogger<LangileZerrendaViewModel> _logger;

    private readonly List<ErabiltzaileLaburpena> _langileIturburuZerrenda = new();

    public LangileZerrendaViewModel(
        AutorizazioZerbitzua autorizazioZerbitzua,
        ErabiltzaileZerbitzua erabiltzaileZerbitzua,
        ILogger<LangileZerrendaViewModel> logger)
    {
        _autorizazioZerbitzua = autorizazioZerbitzua ?? throw new ArgumentNullException(nameof(autorizazioZerbitzua));
        _erabiltzaileZerbitzua = erabiltzaileZerbitzua ?? throw new ArgumentNullException(nameof(erabiltzaileZerbitzua));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [ObservableProperty]
    private bool _isKargatzean;

    [ObservableProperty]
    private string? _erroreMezua;

    [ObservableProperty]
    private string _hutsaMezua = string.Empty;

    [ObservableProperty]
    private string _bilaketaTestua = string.Empty;

    public ObservableCollection<ErabiltzaileLaburpena> Langileak { get; } = new();

    partial void OnBilaketaTestuaChanged(string value)
    {
        EzarriIkuspegiaBilaketarekin();
    }

    private void EzarriIkuspegiaBilaketarekin()
    {
        Langileak.Clear();
        var hitza = BilaketaTestua.Trim();

        IEnumerable<ErabiltzaileLaburpena> hautatuak = _langileIturburuZerrenda;
        if (hitza.Length > 0)
        {
            var kultura = StringComparison.OrdinalIgnoreCase;
            hautatuak = _langileIturburuZerrenda.Where(l =>
                (l.Izena.Contains(hitza, kultura)) ||
                (l.Abizena.Contains(hitza, kultura)) ||
                (l.Posta.Contains(hitza, kultura)) ||
                l.Id.ToString(CultureInfo.InvariantCulture).Contains(hitza, kultura));
        }

        foreach (var lerroa in hautatuak)
            Langileak.Add(lerroa);

        EguneratuHutsaMezua();
    }

    private void EguneratuHutsaMezua()
    {
        if (_langileIturburuZerrenda.Count == 0)
            HutsaMezua = "Oraindik ez dago langilerik erregistratuta.";
        else if (Langileak.Count == 0)
            HutsaMezua = "Ez dago emaitzarik bilaketarekin.";
        else
            HutsaMezua = string.Empty;
    }

    [RelayCommand]
    private async Task AgertzenDeneanAsync()
    {
        ErroreMezua = null;
        HutsaMezua = string.Empty;
        Langileak.Clear();
        _langileIturburuZerrenda.Clear();

        try
        {
            IsKargatzean = true;
            if (!await _autorizazioZerbitzua.DaAdministratzaileaAsync().ConfigureAwait(true))
            {
                ErroreMezua = "Ez duzu baimenik atal honetan.";
                return;
            }

            var adminId = await _autorizazioZerbitzua.EskuratuOraingoErabiltzaileIdAsync().ConfigureAwait(true);
            var zerrenda = adminId is { } aid
                ? await _erabiltzaileZerbitzua.EskuratuLangileenLaburpenakAdministratzailearentzatAsync(aid).ConfigureAwait(true)
                : Array.Empty<ErabiltzaileLaburpena>();
            foreach (var lerroa in zerrenda)
                _langileIturburuZerrenda.Add(lerroa);

            EzarriIkuspegiaBilaketarekin();
        }
        catch (TursoExekuzioSalbuespena libEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                ?? "Datu-base errorea: ezin izan da irakurri. Saiatu berriro.";
            _logger.LogError(libEx, "Langile zerrenda: Turso/libSQL errorea.");
        }
        catch (KeyNotFoundException knfEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ZutabeEskemaMezua;
            _logger.LogError(knfEx, "Langile zerrenda: zutabe edo mapa errorea.");
        }
        catch (FormatException fmtEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.BalioFormatuMezua;
            _logger.LogError(fmtEx, "Langile zerrenda: balio formatu errorea.");
        }
        catch (SQLiteException sqlEx)
        {
            ErroreMezua = "Datu-base errorea: ezin izan da irakurri. Saiatu berriro.";
            _logger.LogError(sqlEx, "Langile zerrenda: SQLite errorea.");
        }
        catch (HttpRequestException httpEx)
        {
            ErroreMezua = "Sare errorea: konexioa egiaztatu eta saiatu berriro.";
            _logger.LogError(httpEx, "Langile zerrenda: sare errorea (Turso?).");
        }
        catch (InvalidOperationException opEx)
        {
            ErroreMezua = "Eragiketa baliogabea. Berriz saiatu saioa hasita.";
            _logger.LogError(opEx, "Langile zerrenda: eragiketa baliogabea.");
        }
        catch (TaskCanceledException)
        {
            ErroreMezua = "Eskaerak denbora muga gainditu du. Saiatu berriro.";
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "Langile zerrenda: ustekabeko errorea.");
        }
        finally
        {
            IsKargatzean = false;
        }
    }

    [RelayCommand]
    private async Task LangileBerriaIrekiAsync()
    {
        await Shell.Current.GoToAsync(nameof(ErabiltzaileBerriaOrria)).ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task IrekiXehetasunaAsync(ErabiltzaileLaburpena? laburpena)
    {
        if (laburpena is null)
            return;

        await Shell.Current
            .GoToAsync($"{nameof(ErabiltzaileXehetasunOrria)}?ErabiltzaileId={laburpena.Id}")
            .ConfigureAwait(true);
    }
}
