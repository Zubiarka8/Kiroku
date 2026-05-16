using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.DatuEreduak;
using Kirokuu.AplikazioZerbitzuak;
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

        SektoreaKargoHautapenLaguntzailea.BeteKargoenZerrenda(KargoenAukerak, value);
        HautatutakoKargoa = KargoenAukerak.FirstOrDefault();
    }

    [ObservableProperty]
    private string _erabiltzaileIdQuery = string.Empty;

    partial void OnErabiltzaileIdQueryChanged(string value)
    {
        if (!ShellQueryLaguntzailea.SaiatuParseatuId(value, out var id))
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

            var adminId = await _autorizazioZerbitzua.EskuratuOraingoErabiltzaileIdAsync().ConfigureAwait(true);
            if (adminId is { } aid &&
                !await _erabiltzaileZerbitzua.AdministratzaileakErabiltzaileaIkusiDezakeAsync(aid, _erabiltzaileIdZenbakia).ConfigureAwait(true))
            {
                ErroreMezua = "Ez duzu baimenik erabiltzaile hau ikusteko.";
                return;
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
        catch (Exception ex)
        {
            ViewModelSalbuespenTratatzailea.TratatuIrakurketa(ex, m => ErroreMezua = m, _logger, "Erabiltzaile xehetasuna");
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
                    HautatutakoSektorea.Identifikatzailea,
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
        catch (Exception ex)
        {
            ViewModelSalbuespenTratatzailea.TratatuIdazketa(ex, m => ErroreMezua = m, _logger, "Erabiltzaile xehetasuna gorde");
        }
        finally
        {
            IsKargatzean = false;
        }
    }

    private void HasieratuSektoreaKargoHautapenak(Erabiltzailea erabiltzailea)
    {
        var sId = erabiltzailea.SektorearenIdentifikatzailea;
        var kId = 0;
        var kTestua = erabiltzailea.Kargoa;
        SektoreaKargoarenHiztegia.SaiatuLeheneratuTestutik(kTestua, ref sId, ref kId);

        _barneratzen = true;
        try
        {
            HautatutakoSektorea = SektoreaKargoHautapenLaguntzailea.BilatuIdentifikatzaileaz(SektoreenAukerak, sId)
                ?? SektoreenAukerak.FirstOrDefault();
            SektoreaKargoHautapenLaguntzailea.BeteKargoenZerrenda(KargoenAukerak, HautatutakoSektorea);
            HautatutakoKargoa = SektoreaKargoHautapenLaguntzailea.BilatuIdentifikatzaileaz(KargoenAukerak, kId)
                ?? KargoenAukerak.FirstOrDefault();
        }
        finally
        {
            _barneratzen = false;
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
