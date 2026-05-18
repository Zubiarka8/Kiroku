using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.DatuEreduak;
using Kirokuu.Zerbitzuak;
using Kirokuu.ZerbitzuakSaioa;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using SQLite;
using System.Linq;
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

    private bool _barneratzen;

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

        foreach (var s in SektoreaKargoarenHiztegia.SortuSektoreenZerrenda())
            SektoreenAukerak.Add(s);
    }

    public ObservableCollection<HautapenElementua> SektoreenAukerak { get; } = new();

    public ObservableCollection<HautapenElementua> KargoenAukerak { get; } = new();

    [ObservableProperty] private HautapenElementua? _hautatutakoSektorea;

    [ObservableProperty] private HautapenElementua? _hautatutakoKargoa;

    partial void OnHautatutakoSektoreaChanged(HautapenElementua? value)
    {
        if (_barneratzen)
            return;

        SektoreaKargoHautapenLaguntzailea.BeteKargoenZerrenda(KargoenAukerak, value?.Etiketa);
        HautatutakoKargoa = KargoenAukerak.FirstOrDefault();
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
    private bool _aktiboa;

    [ObservableProperty]
    private bool _isIkusketaSoilik;

    public bool DaIdazketaSarbidea => !IsIkusketaSoilik;

    partial void OnIsIkusketaSoilikChanged(bool value)
    {
        OnPropertyChanged(nameof(DaIdazketaSarbidea));
        GordeCommand.NotifyCanExecuteChanged();
        DesaktibatuCommand.NotifyCanExecuteChanged();
    }

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
            var daAdministratzailea = await _autorizazioZerbitzua.DaAdministratzaileaAsync().ConfigureAwait(true);
            var daZuzendariNagusia = await _autorizazioZerbitzua.DaZuzendariNagusiaAsync().ConfigureAwait(true);
            if (!daAdministratzailea && !daZuzendariNagusia)
            {
                ErroreMezua = "Ez duzu baimenik.";
                return;
            }

            IsIkusketaSoilik = daZuzendariNagusia && !daAdministratzailea;

            var erabiltzailea = await _erabiltzaileZerbitzua.EskuratuErabiltzaileaIdzAsync(_erabiltzaileIdZenbakia).ConfigureAwait(true);
            if (erabiltzailea is null)
            {
                ErroreMezua = "Erabiltzailea ez da aurkitu.";
                return;
            }

            if (!IsIkusketaSoilik && erabiltzailea.Rola != (int)ErabiltzaileRola.Langilea)
            {
                ErroreMezua = "Administratzaile profilak ezin dira hemen editatu.";
                return;
            }

            if (daAdministratzailea)
            {
                var adminId = await _autorizazioZerbitzua.EskuratuOraingoErabiltzaileIdAsync().ConfigureAwait(true);
                if (adminId is { } aid &&
                    !await _erabiltzaileZerbitzua.AdministratzaileakErabiltzaileaIkusiDezakeAsync(aid, _erabiltzaileIdZenbakia).ConfigureAwait(true))
                {
                    ErroreMezua = "Ez duzu baimenik erabiltzaile hau ikusteko.";
                    return;
                }
            }

            Izena = erabiltzailea.Izena;
            Abizena = erabiltzailea.Abizena;
            Abizena2 = erabiltzailea.Abizena2;
            Dni = erabiltzailea.DNI;
            Posta = erabiltzailea.Posta;
            Aktiboa = erabiltzailea.Aktiboa != 0;

            HasieratuSektoreaKargoHautapenak(erabiltzailea);
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

    [RelayCommand(CanExecute = nameof(DaIdazketaSarbidea))]
    private async Task GordeAsync()
    {
        ErroreMezua = null;
        if (_erabiltzaileIdZenbakia <= 0 || IsIkusketaSoilik)
            return;

        if (!await _autorizazioZerbitzua.DaAdministratzaileaAsync().ConfigureAwait(true))
        {
            ErroreMezua = "Ez duzu baimenik.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Izena) || string.IsNullOrWhiteSpace(Abizena) ||
            string.IsNullOrWhiteSpace(Abizena2) || string.IsNullOrWhiteSpace(Dni) ||
            HautatutakoSektorea is null || HautatutakoKargoa is null ||
            string.IsNullOrWhiteSpace(Posta))
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
                HautatutakoSektorea.Etiketa, HautatutakoKargoa.Identifikatzailea))
        {
            ErroreMezua = "Hautatu sektore eta kargo baliodunak.";
            return;
        }

        if (!ErabiltzaileDatuenBalidazioLaguntzailea.PostaBaliozkoa(Posta))
        {
            ErroreMezua = "Posta helbidearen formatua ez da zuzena.";
            return;
        }

        var adminIdGordetzean = await _autorizazioZerbitzua.EskuratuOraingoErabiltzaileIdAsync().ConfigureAwait(true);
        if (adminIdGordetzean is { } aidGordetzean &&
            !await _erabiltzaileZerbitzua.AdministratzaileakSektoreaKudeatuDezakeAsync(
                aidGordetzean, HautatutakoSektorea.Identifikatzailea).ConfigureAwait(true))
        {
            ErroreMezua = "Ez duzu baimenik beste sektore baten erabiltzailea kudeatzeko.";
            return;
        }

        try
        {
            IsKargatzean = true;
            if (await _erabiltzaileZerbitzua.NANErabilitaDagoaAsync(Dni, _erabiltzaileIdZenbakia).ConfigureAwait(true))
            {
                ErroreMezua = "NAN hau dagoeneko beste erabiltzaile bati esleituta dago.";
                return;
            }

            await _erabiltzaileZerbitzua.AdministratzaileakEguneratuErabiltzaileProfilaAsync(
                    _erabiltzaileIdZenbakia,
                    Izena,
                    Abizena,
                    Abizena2,
                    Dni,
                    Posta,
                    HautatutakoSektorea.Etiketa,
                    HautatutakoKargoa.Identifikatzailea,
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

    private void HasieratuSektoreaKargoHautapenak(Erabiltzailea erabiltzailea)
    {
        string? sektoreIzena = erabiltzailea.Sektorea;
        var kId = 0;
        var kTestua = erabiltzailea.Kargoa;
        if (!SektoreaKargoarenHiztegia.SektoreaEtaKargoarenIdentifikatzaileakBaliozkoa(sektoreIzena, kId))
            SektoreaKargoarenHiztegia.SaiatuLeheneratuTestutik(kTestua, ref sektoreIzena, ref kId);

        _barneratzen = true;
        try
        {
            HautatutakoSektorea = SektoreenAukerak.FirstOrDefault(x => string.Equals(x.Etiketa, sektoreIzena, StringComparison.Ordinal))
                ?? SektoreenAukerak.FirstOrDefault();
            SektoreaKargoHautapenLaguntzailea.BeteKargoenZerrenda(KargoenAukerak, HautatutakoSektorea?.Etiketa);
            HautatutakoKargoa = SektoreaKargoHautapenLaguntzailea.BilatuIdentifikatzaileaz(KargoenAukerak, kId)
                ?? KargoenAukerak.FirstOrDefault();
        }
        finally
        {
            _barneratzen = false;
        }
    }

    [RelayCommand(CanExecute = nameof(DaIdazketaSarbidea))]
    private async Task DesaktibatuAsync()
    {
        ErroreMezua = null;
        if (_erabiltzaileIdZenbakia <= 0 || IsIkusketaSoilik)
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
