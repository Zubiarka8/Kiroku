using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.DatuEreduak;
using Kirokuu.Pages;
using Kirokuu.Zerbitzuak;
using Kirokuu.ZerbitzuakSaioa;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
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

    private string _administratzailearenSektorea = string.Empty;

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

    [ObservableProperty]
    private bool _isIkusketaSoilik;

    public bool DaIdazketaSarbidea => !IsIkusketaSoilik;

    partial void OnIsIkusketaSoilikChanged(bool value)
    {
        OnPropertyChanged(nameof(DaIdazketaSarbidea));
        LangileBerriaIrekiCommand.NotifyCanExecuteChanged();
    }

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
            var daAdministratzailea = await _autorizazioZerbitzua.DaAdministratzaileaAsync().ConfigureAwait(true);
            var daZuzendariNagusia = await _autorizazioZerbitzua.DaZuzendariNagusiaAsync().ConfigureAwait(true);
            if (!daAdministratzailea && !daZuzendariNagusia)
            {
                ErroreMezua = "Ez duzu baimenik atal honetan.";
                return;
            }

            IsIkusketaSoilik = daZuzendariNagusia && !daAdministratzailea;

            var oraingoId = await _autorizazioZerbitzua.EskuratuOraingoErabiltzaileIdAsync().ConfigureAwait(true);
            if (oraingoId is null)
            {
                ErroreMezua = "Ezin izan da uneko erabiltzailea identifikatu.";
                return;
            }

            IReadOnlyList<ErabiltzaileLaburpena> zerrenda;
            if (IsIkusketaSoilik)
            {
                // CEO: sektorerik gabe, erabiltzaile guztiak (administratzaileak barne) ikusgai.
                _administratzailearenSektorea = string.Empty;
                zerrenda = await _erabiltzaileZerbitzua
                    .EskuratuLangileenLaburpenakAsync(null, soilikLangileak: false)
                    .ConfigureAwait(true);
            }
            else
            {
                var administratzailea = await _erabiltzaileZerbitzua.EskuratuErabiltzaileaIdzAsync(oraingoId.Value).ConfigureAwait(true);
                var sektorea = administratzailea?.Sektorea?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(sektorea) ||
                    !SektoreaKargoarenHiztegia.SortuSektoreenZerrenda().Any(s => string.Equals(s.Etiketa, sektorea, StringComparison.Ordinal)))
                {
                    ErroreMezua = "Zure profilak ez du sektore baliodunik. Ezarri sektorea Ezarpenetan.";
                    return;
                }

                _administratzailearenSektorea = sektorea;
                zerrenda = await _erabiltzaileZerbitzua
                    .EskuratuLangileenLaburpenakAdministratzailearentzatAsync(oraingoId.Value)
                    .ConfigureAwait(true);
            }

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

    [RelayCommand(CanExecute = nameof(DaIdazketaSarbidea))]
    private async Task LangileBerriaIrekiAsync()
    {
        await Shell.Current.GoToAsync(nameof(ErabiltzaileBerriaOrria)).ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task IrekiEkintzakAsync(ErabiltzaileLaburpena? laburpena)
    {
        if (laburpena is null)
            return;

        if (IsIkusketaSoilik)
        {
            // CEO: irakurketa hutsa, xehetasunetara zuzenean.
            await Shell.Current
                .GoToAsync($"{nameof(ErabiltzaileXehetasunOrria)}?ErabiltzaileId={laburpena.Id.ToString(CultureInfo.InvariantCulture)}")
                .ConfigureAwait(true);
            return;
        }

        if (!string.IsNullOrEmpty(_administratzailearenSektorea) &&
            !string.Equals(laburpena.Sektorea?.Trim(), _administratzailearenSektorea, StringComparison.Ordinal))
        {
            ErroreMezua = "Ez duzu baimenik beste sektore bateko erabiltzaileak kudeatzeko.";
            return;
        }

        var dagoeneko = laburpena.Aktiboa != 0;
        const string xehetasunakEtiketa = "Xehetasunak ireki";
        var aldaketaEtiketa = dagoeneko ? "Desaktibatu" : "Aktibatu";
        var izenburua = string.Concat(laburpena.Izena, " ", laburpena.Abizena).Trim();

        var hautatua = await Shell.Current
            .DisplayActionSheet(izenburua, "Utzi", null, xehetasunakEtiketa, aldaketaEtiketa)
            .ConfigureAwait(true);

        if (string.IsNullOrEmpty(hautatua) || string.Equals(hautatua, "Utzi", StringComparison.Ordinal))
            return;

        if (string.Equals(hautatua, xehetasunakEtiketa, StringComparison.Ordinal))
        {
            await Shell.Current
                .GoToAsync($"{nameof(ErabiltzaileXehetasunOrria)}?ErabiltzaileId={laburpena.Id.ToString(CultureInfo.InvariantCulture)}")
                .ConfigureAwait(true);
            return;
        }

        if (!string.Equals(hautatua, aldaketaEtiketa, StringComparison.Ordinal))
            return;

        await AldatuAktiboaAsync(laburpena, dagoeneko ? 0 : 1).ConfigureAwait(true);
    }

    private async Task AldatuAktiboaAsync(ErabiltzaileLaburpena laburpena, int aktiboBerria)
    {
        try
        {
            IsKargatzean = true;
            await _erabiltzaileZerbitzua.EguneratuErabiltzaileaAktiboaAsync(laburpena.Id, aktiboBerria).ConfigureAwait(true);

            var mezua = aktiboBerria == 0 ? "Erabiltzailea desaktibatu da." : "Erabiltzailea aktibatu da.";
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await BokadilloErakustzailea.SaiatuErakutsiAsync(mezua, _logger).ConfigureAwait(true);
            }).ConfigureAwait(true);

            await AgertzenDeneanAsync().ConfigureAwait(true);
        }
        catch (TursoExekuzioSalbuespena libEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                ?? "Datu-base errorea: ezin izan da eguneratu. Saiatu berriro.";
            _logger.LogError(libEx, "Aktiboa aldaketa: Turso errorea.");
        }
        catch (SQLiteException sqlEx)
        {
            ErroreMezua = "Datu-base errorea: ezin izan da eguneratu. Saiatu berriro.";
            _logger.LogError(sqlEx, "Aktiboa aldaketa: SQLite errorea.");
        }
        catch (HttpRequestException httpEx)
        {
            ErroreMezua = "Sare errorea: konexioa egiaztatu eta saiatu berriro.";
            _logger.LogError(httpEx, "Aktiboa aldaketa: sare errorea.");
        }
        catch (InvalidOperationException opEx)
        {
            ErroreMezua = "Eragiketa baliogabea. Berriz saiatu saioa hasita.";
            _logger.LogError(opEx, "Aktiboa aldaketa: eragiketa baliogabea.");
        }
        catch (TaskCanceledException)
        {
            ErroreMezua = "Eskaerak denbora muga gainditu du. Saiatu berriro.";
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "Aktiboa aldaketa: ustekabeko errorea.");
        }
        finally
        {
            IsKargatzean = false;
        }
    }
}
