using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.DatuEreduak;
using Kirokuu.Zerbitzuak;
using Kirokuu.ZerbitzuakSaioa;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using SQLite;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace Kirokuu.ViewModels;

public partial class ErregistroViewModel : ObservableObject
{
    private bool _barneratzen;

    private readonly ErabiltzaileZerbitzua _erabiltzaileZerbitzua;
    private readonly SaioaGordetzeZerbitzua _saioaGordetzeZerbitzua;
    private readonly INabigazioNagusia _nabigazioNagusia;
    private readonly ILogger<ErregistroViewModel> _logger;

    public ErregistroViewModel(
        ErabiltzaileZerbitzua erabiltzaileZerbitzua,
        SaioaGordetzeZerbitzua saioaGordetzeZerbitzua,
        INabigazioNagusia nabigazioNagusia,
        ILogger<ErregistroViewModel> logger)
    {
        _erabiltzaileZerbitzua = erabiltzaileZerbitzua ?? throw new ArgumentNullException(nameof(erabiltzaileZerbitzua));
        _saioaGordetzeZerbitzua = saioaGordetzeZerbitzua ?? throw new ArgumentNullException(nameof(saioaGordetzeZerbitzua));
        _nabigazioNagusia = nabigazioNagusia ?? throw new ArgumentNullException(nameof(nabigazioNagusia));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        foreach (var s in SektoreaKargoarenHiztegia.SortuSektoreenZerrenda())
            SektoreenAukerak.Add(s);

        _barneratzen = true;
        try
        {
            HautatutakoSektorea = SektoreenAukerak.FirstOrDefault();
            SektoreaKargoHautapenLaguntzailea.BeteKargoenZerrenda(KargoenAukerak, HautatutakoSektorea);
            HautatutakoKargoa = KargoenAukerak.FirstOrDefault();
        }
        finally
        {
            _barneratzen = false;
        }
    }

    public ObservableCollection<HautapenElementua> SektoreenAukerak { get; } = new();

    public ObservableCollection<HautapenElementua> KargoenAukerak { get; } = new();

    [ObservableProperty] private HautapenElementua? _hautatutakoSektorea;

    [ObservableProperty] private HautapenElementua? _hautatutakoKargoa;

    partial void OnHautatutakoSektoreaChanged(HautapenElementua? value)
    {
        if (_barneratzen)
            return;

        SektoreaKargoHautapenLaguntzailea.BeteKargoenZerrenda(KargoenAukerak, value);
        HautatutakoKargoa = KargoenAukerak.FirstOrDefault();
    }

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
    private string _pasahitza = string.Empty;

    [ObservableProperty]
    private string _pasahitzaBerretsi = string.Empty;

    [ObservableProperty]
    private bool _isKargatzean;

    [ObservableProperty]
    private string? _erroreMezua;

    [ObservableProperty]
    private string? _erroreXehetasuna;

    [ObservableProperty]
    private bool _pasahitzaMaskaratuta = true;

    [ObservableProperty]
    private string _pasahitzaBegiarenIrudiarenIzena = "begia_irekita";

    [ObservableProperty]
    private string _pasahitzaBegiarenDeskribapena = "Erakutsi pasahitza";

    [ObservableProperty]
    private bool _pasahitzaBerretsiMaskaratuta = true;

    [ObservableProperty]
    private string _pasahitzaBerretsiBegiarenIrudiarenIzena = "begia_irekita";

    [ObservableProperty]
    private string _pasahitzaBerretsiBegiarenDeskribapena = "Erakutsi pasahitza";

    partial void OnPasahitzaMaskaratutaChanged(bool value)
    {
        PasahitzaBegiarenIrudiarenIzena = value ? "begia_irekita" : "begia_itxita";
        PasahitzaBegiarenDeskribapena = value ? "Erakutsi pasahitza" : "Ezkutatu pasahitza";
    }

    partial void OnPasahitzaBerretsiMaskaratutaChanged(bool value)
    {
        PasahitzaBerretsiBegiarenIrudiarenIzena = value ? "begia_irekita" : "begia_itxita";
        PasahitzaBerretsiBegiarenDeskribapena = value ? "Erakutsi pasahitza berretsia" : "Ezkutatu pasahitza berretsia";
    }

    [RelayCommand]
    private void AlderantzikatuPasahitzaMaska() => PasahitzaMaskaratuta = !PasahitzaMaskaratuta;

    [RelayCommand]
    private void AlderantzikatuPasahitzaBerretsiMaska() => PasahitzaBerretsiMaskaratuta = !PasahitzaBerretsiMaskaratuta;

    [RelayCommand]
    private async Task ErregistratuAsync()
    {
        ErroreMezua = null;
        ErroreXehetasuna = null;
        if (string.IsNullOrWhiteSpace(Izena) || string.IsNullOrWhiteSpace(Abizena) ||
            string.IsNullOrWhiteSpace(Abizena2) || string.IsNullOrWhiteSpace(Dni) ||
            HautatutakoSektorea is null || HautatutakoKargoa is null ||
            string.IsNullOrWhiteSpace(Posta) || string.IsNullOrWhiteSpace(Pasahitza) ||
            string.IsNullOrWhiteSpace(PasahitzaBerretsi))
        {
            ErroreMezua = "Eremu bat edo gehiago hutsik daude. Bete beharrezko eremuak.";
            return;
        }

        if (!ErabiltzaileDatuenBalidazioLaguntzailea.PertsonaIzenLaburraBaliozkoa(Izena, 2, 80) ||
            !ErabiltzaileDatuenBalidazioLaguntzailea.PertsonaIzenLaburraBaliozkoa(Abizena, 2, 80) ||
            !ErabiltzaileDatuenBalidazioLaguntzailea.PertsonaIzenLaburraBaliozkoa(Abizena2, 2, 80))
        {
            ErroreMezua = "Izen edo abizenak ez dira zuzenak (letrak eta tarteak soilik, 2–80 karaktere).";
            return;
        }

        if (!ErabiltzaileDatuenBalidazioLaguntzailea.NanEdoIfzBaliozkoa(Dni))
        {
            ErroreMezua = "NAN / IFZ zenbakia ez da zuzena (8 zenbaki + letra, edo X/Y/Z + 7 zenbaki + letra).";
            return;
        }

        if (!SektoreaKargoarenHiztegia.SektoreaEtaKargoarenIdentifikatzaileakBaliozkoa(
                HautatutakoSektorea.Identifikatzailea, HautatutakoKargoa.Identifikatzailea))
        {
            ErroreMezua = "Hautatu sektore eta kargo baliodunak.";
            return;
        }

        if (!ErabiltzaileDatuenBalidazioLaguntzailea.PostaBaliozkoa(Posta))
        {
            ErroreMezua = "Posta helbidearen formatua ez da zuzena.";
            return;
        }

        if (Pasahitza.Length < 8)
        {
            ErroreMezua = "Pasahitzak gutxienez 8 karaktere izan behar ditu.";
            return;
        }

        if (!string.Equals(Pasahitza, PasahitzaBerretsi, StringComparison.Ordinal))
        {
            ErroreMezua = "Pasahitzak ez datoz bat.";
            return;
        }

        try
        {
            IsKargatzean = true;
            if (await _erabiltzaileZerbitzua.NANErabilitaDagoaAsync(Dni).ConfigureAwait(true))
            {
                ErroreMezua = "NAN hau dagoeneko erregistratuta dago.";
                return;
            }

            var erabiltzailea = await _erabiltzaileZerbitzua.ErregistratuLangileaAsync(
                Izena,
                Abizena,
                Abizena2,
                Dni,
                HautatutakoSektorea.Identifikatzailea,
                HautatutakoKargoa.Identifikatzailea,
                Posta,
                Pasahitza).ConfigureAwait(true);
            await _saioaGordetzeZerbitzua.GordeAsync(erabiltzailea).ConfigureAwait(true);
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await BokadilloErakustzailea.SaiatuErakutsiAsync("Kontua sortu da. Ongi etorri!", _logger)
                    .ConfigureAwait(true);
            }).ConfigureAwait(true);
            await _nabigazioNagusia.JoanAppShelleraAsync().ConfigureAwait(true);
        }
        catch (ErabiltzaileMurrizketaSalbuespena murEx)
        {
            ErroreXehetasuna = null;
            ErroreMezua = "Datu bikoiztua: posta hau dagoeneko erregistratuta dago.";
            _logger.LogWarning(murEx, "Erregistroa: murrizketa (Turso).");
        }
        catch (SQLiteException sqlEx) when (sqlEx.Result == SQLite3.Result.Constraint)
        {
            var (eskN, eskX) = DatuBaseaErroreaErabiltzaileMezura.EskuratuSqliteMezuak(sqlEx.Message);
            ErroreMezua = eskN ?? "Datu bikoiztua: posta hau dagoeneko erregistratuta dago.";
            ErroreXehetasuna = eskX;
            _logger.LogWarning(sqlEx, "Erregistroa: murrizketa urratua.");
        }
        catch (TursoExekuzioSalbuespena libEx)
        {
            var (nagusia, xehetasuna) = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezuaXehetasunarekin(libEx);
            ErroreMezua = nagusia ?? "Datu-base errorea: ezin izan da gorde. Saiatu berriro.";
            ErroreXehetasuna = xehetasuna;
            _logger.LogError(libEx, "Erregistroa: Turso/libSQL errorea.");
        }
        catch (KeyNotFoundException knfEx)
        {
            ErroreXehetasuna = null;
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ZutabeEskemaMezua;
            _logger.LogError(knfEx, "Erregistroa: zutabe edo mapa errorea.");
        }
        catch (FormatException fmtEx)
        {
            ErroreXehetasuna = null;
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.BalioFormatuMezua;
            _logger.LogError(fmtEx, "Erregistroa: balio formatu errorea.");
        }
        catch (SQLiteException sqlEx)
        {
            var (eskN, eskX) = DatuBaseaErroreaErabiltzaileMezura.EskuratuSqliteMezuak(sqlEx.Message);
            if (eskN is not null)
            {
                ErroreMezua = eskN;
                ErroreXehetasuna = eskX;
            }
            else
            {
                ErroreXehetasuna = null;
                ErroreMezua = "Datu-base errorea: ezin izan da gorde. Saiatu berriro.";
            }

            _logger.LogError(sqlEx, "Erregistroa: SQLite errorea.");
        }
        catch (HttpRequestException httpEx)
        {
            ErroreXehetasuna = null;
            ErroreMezua = "Sare errorea: konexioa egiaztatu eta saiatu berriro.";
            _logger.LogError(httpEx, "Erregistroa: sare errorea (Turso?).");
        }
        catch (TaskCanceledException)
        {
            ErroreXehetasuna = null;
            ErroreMezua = "Eskaerak denbora muga gainditu du. Saiatu berriro.";
        }
        catch (OperationCanceledException)
        {
            ErroreXehetasuna = null;
            ErroreMezua = "Eskaerak denbora muga gainditu du edo eragiketa ezeztatu da. Saiatu berriro.";
        }
        catch (InvalidOperationException opEx)
        {
            ErroreXehetasuna = null;
            ErroreMezua = "Eragiketa baliogabea. Berriz saiatu saioa hasita.";
            _logger.LogError(opEx, "Erregistroa: nabigazio errorea.");
        }
        catch (ArgumentException argEx)
        {
            ErroreXehetasuna = null;
            ErroreMezua = "Ezin izan da saioaren datuak gorde. Saiatu berriro edo berrabiarazi aplikazioa.";
            _logger.LogError(argEx, "Erregistroa: SecureStorage edo balio baliogabea.");
        }
        catch (NotSupportedException nsEx)
        {
            ErroreXehetasuna = null;
            ErroreMezua = "Datu-base errorea: eragiketa ez da onartzen. Saiatu berriro.";
            _logger.LogError(nsEx, "Erregistroa: onartzen ez den eragiketa.");
        }
        catch (UnauthorizedAccessException uaaEx)
        {
            ErroreXehetasuna = null;
            ErroreMezua = "Baimena ukatu da. Ezarpenetan baimena eman.";
            _logger.LogError(uaaEx, "Erregistroa: baimena ukatua (SecureStorage edo sistema).");
        }
        catch (IOException ioEx)
        {
            ErroreXehetasuna = null;
            ErroreMezua = "Fitxategi errorea: ezin izan dira saioaren datuak gorde.";
            _logger.LogError(ioEx, "Erregistroa: fitxategi errorea (SecureStorage?).");
        }
        catch (AggregateException aggEx)
        {
            var (nAg, xAg) = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezuaXehetasunarekin(aggEx);
            if (nAg is not null)
            {
                ErroreMezua = nAg;
                ErroreXehetasuna = xAg;
            }
            else
            {
                ErroreMezua = LibsqlErroreaErabiltzaileMezura.AgregatuarenBarnekoErabiltzaileMezua(aggEx)
                    ?? "Datu-base edo sare errorea: saiatu berriro.";
                ErroreXehetasuna = null;
            }

            _logger.LogError(aggEx, "Erregistroa: salbuespen agregatua.");
        }
        catch (Exception ex)
        {
            var (nagusia, xehetasuna) = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezuaXehetasunarekin(ex);
            if (nagusia is not null)
            {
                ErroreMezua = nagusia;
                ErroreXehetasuna = xehetasuna;
            }
            else
            {
                ErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(ex)
                    ?? "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
                ErroreXehetasuna = null;
            }

            _logger.LogError(ex, "Erregistroa: ustekabeko errorea.");
        }
        finally
        {
            IsKargatzean = false;
        }
    }

    [RelayCommand]
    private async Task ItzuliAsync()
    {
        ErroreMezua = null;
        ErroreXehetasuna = null;
        try
        {
            if (Application.Current?.Windows.FirstOrDefault()?.Page is NavigationPage nabigazioa &&
                nabigazioa.Navigation.NavigationStack.Count > 1)
                await nabigazioa.PopAsync().ConfigureAwait(true);
        }
        catch (InvalidOperationException opEx)
        {
            ErroreMezua = "Eragiketa baliogabea. Berriz saiatu saioa hasita.";
            _logger.LogError(opEx, "Erregistroa: itzultze errorea.");
        }
        catch (UnauthorizedAccessException uaaEx)
        {
            ErroreMezua = "Baimena ukatu da. Ezarpenetan baimena eman.";
            _logger.LogError(uaaEx, "Erregistroa: itzultzean baimena ukatua.");
        }
        catch (IOException ioEx)
        {
            ErroreMezua = "Fitxategi errorea: ezin izan da itzuli.";
            _logger.LogError(ioEx, "Erregistroa: itzultzean fitxategi errorea.");
        }
        catch (AggregateException aggEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.AgregatuarenBarnekoErabiltzaileMezua(aggEx)
                ?? "Datu-base edo sare errorea: saiatu berriro.";
            _logger.LogError(aggEx, "Erregistroa: itzultzean salbuespen agregatua.");
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "Erregistroa: itzultzean ustekabeko errorea.");
        }
    }
}
