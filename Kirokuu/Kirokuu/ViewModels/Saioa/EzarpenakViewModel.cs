using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.DatuEreduak;
using Kirokuu.Zerbitzuak;
using Kirokuu.ZerbitzuakSaioa;
using Microsoft.Extensions.Logging;
using SQLite;
using System.Linq;
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

    private int? _erabiltzaileId;

    private bool _barneratzen;

    private bool _daAdministratzaileTaldea;

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

    [ObservableProperty] private bool _isKargatzean;
    [ObservableProperty] private string? _erroreMezua;

    [ObservableProperty] private string _izena = string.Empty;
    [ObservableProperty] private string _abizena = string.Empty;
    [ObservableProperty] private string _abizena2 = string.Empty;
    [ObservableProperty] private string _dNI = string.Empty;
    [ObservableProperty] private string _posta = string.Empty;
    [ObservableProperty] private string _rolTestu = string.Empty;
    [ObservableProperty] private string _sorkuntzaDataTestu = string.Empty;

    [ObservableProperty] private bool _daAdministratzailea;

    [ObservableProperty] private bool _erakutsiSektoreaKargoHautapenak = true;

    [ObservableProperty] private string _finkatutakoSektorearenEtiketa = string.Empty;

    [ObservableProperty] private string _finkatutakoKargoarenEtiketa = string.Empty;

    [ObservableProperty] private string _pasahitzaZaharra = string.Empty;
    [ObservableProperty] private bool _pasahitzaZaharraMaskaratuta = true;
    [ObservableProperty] private string _pasahitzaZaharraBegiIzena = "begia_irekita";
    [ObservableProperty] private string _pasahitzaZaharraBegiDesk = "Erakutsi pasahitza";

    [ObservableProperty] private bool _pasahitzaEgiaztatuta;

    [ObservableProperty] private string _pasahitzaBerria = string.Empty;
    [ObservableProperty] private bool _pasahitzaBerriaMaskaratuta = true;
    [ObservableProperty] private string _pasahitzaBerriaBegiIzena = "begia_irekita";
    [ObservableProperty] private string _pasahitzaBerriaBegiDesk = "Erakutsi pasahitza";

    [ObservableProperty] private string _pasahitzaBerriaBerretsi = string.Empty;
    [ObservableProperty] private bool _pasahitzaBerriaBerretsiMaskaratuta = true;
    [ObservableProperty] private string _pasahitzaBerriaBerretsiBegiIzena = "begia_irekita";
    [ObservableProperty] private string _pasahitzaBerriaBerretsiBegiDesk = "Erakutsi pasahitza";

    [ObservableProperty] private bool _pasahitzaEkintza;
    [ObservableProperty] private string? _pasahitzaErroreMezua;
    [ObservableProperty] private bool _pasahitzaErroreDago;

    [ObservableProperty] private bool _profilaGordetzen;

    partial void OnPasahitzaZaharraMaskaratutaChanged(bool value)
    {
        PasahitzaZaharraBegiIzena = value ? "begia_irekita" : "begia_itxita";
        PasahitzaZaharraBegiDesk = value ? "Erakutsi pasahitza" : "Ezkutatu pasahitza";
    }

    partial void OnPasahitzaBerriaMaskaratutaChanged(bool value)
    {
        PasahitzaBerriaBegiIzena = value ? "begia_irekita" : "begia_itxita";
        PasahitzaBerriaBegiDesk = value ? "Erakutsi pasahitza" : "Ezkutatu pasahitza";
    }

    partial void OnPasahitzaBerriaBerretsiMaskaratutaChanged(bool value)
    {
        PasahitzaBerriaBerretsiBegiIzena = value ? "begia_irekita" : "begia_itxita";
        PasahitzaBerriaBerretsiBegiDesk = value ? "Erakutsi pasahitza" : "Ezkutatu pasahitza";
    }

    partial void OnPasahitzaErroreMezuaChanged(string? value) =>
        PasahitzaErroreDago = !string.IsNullOrEmpty(value);

    [RelayCommand]
    private void AlderantzikatuPasahitzaZaharraMaska() => PasahitzaZaharraMaskaratuta = !PasahitzaZaharraMaskaratuta;

    [RelayCommand]
    private void AlderantzikatuPasahitzaBerriaMaska() => PasahitzaBerriaMaskaratuta = !PasahitzaBerriaMaskaratuta;

    [RelayCommand]
    private void AlderantzikatuPasahitzaBerriaBerretsiMaska() => PasahitzaBerriaBerretsiMaskaratuta = !PasahitzaBerriaBerretsiMaskaratuta;

    [RelayCommand]
    private async Task AgertzenDeneanAsync()
    {
        ErroreMezua = null;
        PasahitzaErroreMezua = null;
        PasahitzaEgiaztatuta = false;
        PasahitzaZaharra = string.Empty;
        PasahitzaBerria = string.Empty;
        PasahitzaBerriaBerretsi = string.Empty;
        try
        {
            IsKargatzean = true;
            var id = await _autorizazioZerbitzua.EskuratuOraingoErabiltzaileIdAsync().ConfigureAwait(true);
            if (id is null)
            {
                ErroreMezua = "Saioa ez da aurkitu. Hasi saioa berriro.";
                return;
            }
            _erabiltzaileId = id.Value;

            var erabiltzailea = await _erabiltzaileZerbitzua.EskuratuErabiltzaileaIdzAsync(id.Value).ConfigureAwait(true);
            if (erabiltzailea is null)
            {
                ErroreMezua = "Erabiltzailearen datuak ez dira aurkitu.";
                return;
            }

            Izena = erabiltzailea.Izena;
            Abizena = erabiltzailea.Abizena;
            Abizena2 = erabiltzailea.Abizena2;
            DNI = erabiltzailea.DNI;
            Posta = erabiltzailea.Posta;
            RolTestu = erabiltzailea.Rola switch
            {
                (int)ErabiltzaileRola.Administratzailea => "Administratzailea",
                (int)ErabiltzaileRola.ZuzendariNagusia => "Zuzendari Nagusia (CEO)",
                _ => "Langilea"
            };

            EzarriSektoreaKargoIkuspegia(erabiltzailea);

            if (DateTime.TryParse(erabiltzailea.SorkuntzaData, null, System.Globalization.DateTimeStyles.RoundtripKind, out var data))
                SorkuntzaDataTestu = data.ToLocalTime().ToString("dd/MM/yyyy");
            else
                SorkuntzaDataTestu = erabiltzailea.SorkuntzaData;
        }
        catch (TursoExekuzioSalbuespena libEx)
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
            _logger.LogError(httpEx, "Ezarpenak: sare errorea.");
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
    private async Task ProfilaGordeAsync()
    {
        ErroreMezua = null;

        if (_erabiltzaileId is null)
        {
            ErroreMezua = "Saioa ez da aurkitu. Hasi saioa berriro.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Posta))
        {
            ErroreMezua = "Eremu bat edo gehiago hutsik daude. Bete beharrezko eremuak.";
            return;
        }

        if (_daAdministratzaileTaldea)
        {
            if (string.IsNullOrWhiteSpace(Izena) || string.IsNullOrWhiteSpace(Abizena) ||
                string.IsNullOrWhiteSpace(Abizena2) || string.IsNullOrWhiteSpace(DNI))
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

            if (!ErabiltzaileDatuenBalidazioLaguntzailea.NanEdoIfzBaliozkoa(DNI))
            {
                ErroreMezua = "NAN / IFZ zenbakia ez da zuzena (8 zenbaki + letra, edo X/Y/Z + 7 zenbaki + letra).";
                return;
            }
        }

        if (!ErabiltzaileDatuenBalidazioLaguntzailea.PostaBaliozkoa(Posta))
        {
            ErroreMezua = "Posta helbidearen formatua ez da zuzena.";
            return;
        }

        try
        {
            ProfilaGordetzen = true;
            var sektoreIzena = _daAdministratzaileTaldea
                ? AdministratzaileOrganizazioLehenetsia.Sektorea
                : HautatutakoSektorea!.Etiketa;
            var kargoId = _daAdministratzaileTaldea
                ? (int)EnpresakoLangileKargoa.AdministratzaileSistema
                : HautatutakoKargoa!.Identifikatzailea;

            await _erabiltzaileZerbitzua.NorberarenProfilaEguneratuAsync(
                    _erabiltzaileId.Value,
                    Izena,
                    Abizena,
                    Abizena2,
                    DNI,
                    Posta,
                    sektoreIzena,
                    kargoId)
                .ConfigureAwait(true);

            await _saioaGordetzeZerbitzua.EguneratuIzenAbizenakSaioanAsync(Izena.Trim(), Abizena.Trim())
                .ConfigureAwait(true);

            var freskoa = await _erabiltzaileZerbitzua.EskuratuErabiltzaileaIdzAsync(_erabiltzaileId.Value).ConfigureAwait(true);
            if (freskoa is not null)
            {
                Izena = freskoa.Izena;
                Abizena = freskoa.Abizena;
                Abizena2 = freskoa.Abizena2;
                DNI = freskoa.DNI;
                Posta = freskoa.Posta;
                EzarriSektoreaKargoIkuspegia(freskoa);
            }

            await BokadilloErakustzailea.SaiatuErakutsiAsync("Profila eguneratu da.", _logger).ConfigureAwait(true);
        }
        catch (ErabiltzaileMurrizketaSalbuespena murEx)
        {
            ErroreMezua = "Posta edo DNI bikoiztua.";
            _logger.LogWarning(murEx, "Ezarpenak: profila murrizketa.");
        }
        catch (SQLiteException sqlEx) when (sqlEx.Result == SQLite3.Result.Constraint)
        {
            ErroreMezua = "Datu bikoiztua: posta edo DNI jadanik erabilita.";
            _logger.LogWarning(sqlEx, "Ezarpenak: profila SQLite murrizketa.");
        }
        catch (SQLiteException sqlEx)
        {
            ErroreMezua = "Datu-base errorea: ezin izan da gorde. Saiatu berriro.";
            _logger.LogError(sqlEx, "Ezarpenak: profila SQLite errorea.");
        }
        catch (TursoExekuzioSalbuespena libEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                ?? "Datu-base errorea: ezin izan da gorde. Saiatu berriro.";
            _logger.LogError(libEx, "Ezarpenak: profila Turso errorea.");
        }
        catch (KeyNotFoundException knfEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ZutabeEskemaMezua;
            _logger.LogError(knfEx, "Ezarpenak: profila mapa errorea.");
        }
        catch (FormatException fmtEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.BalioFormatuMezua;
            _logger.LogError(fmtEx, "Ezarpenak: profila formatu errorea.");
        }
        catch (HttpRequestException httpEx)
        {
            ErroreMezua = "Sare errorea: konexioa egiaztatu eta saiatu berriro.";
            _logger.LogError(httpEx, "Ezarpenak: profila sare errorea.");
        }
        catch (InvalidOperationException opEx)
        {
            ErroreMezua = "Eragiketa baliogabea. Berriz saiatu saioa hasita.";
            _logger.LogError(opEx, "Ezarpenak: profila eragiketa baliogabea.");
        }
        catch (TaskCanceledException)
        {
            ErroreMezua = "Eskaerak denbora muga gainditu du. Saiatu berriro.";
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "Ezarpenak: profila ustekabeko errorea.");
        }
        finally
        {
            ProfilaGordetzen = false;
        }
    }

    [RelayCommand]
    private async Task PasahitzaEgiaztatzAsync()
    {
        PasahitzaErroreMezua = null;
        if (string.IsNullOrWhiteSpace(PasahitzaZaharra))
        {
            PasahitzaErroreMezua = "Sartu zure uneko pasahitza.";
            return;
        }
        if (string.IsNullOrEmpty(Posta))
        {
            PasahitzaErroreMezua = "Profileko datuak ez daude kargatuta. Berrabiarazi orria.";
            return;
        }

        try
        {
            PasahitzaEkintza = true;
            var (mota, _, blokeoaGeratzen) = await _erabiltzaileZerbitzua.SaioaHasiAsync(Posta, PasahitzaZaharra).ConfigureAwait(true);
            if (mota == SaioHasieraEmaitzaMota.Ongi)
                PasahitzaEgiaztatuta = true;
            else if (mota == SaioHasieraEmaitzaMota.SaioDenborazBlokeatuta)
            {
                var geratzen = blokeoaGeratzen ?? TimeSpan.FromMinutes(ErabiltzaileZerbitzua.SaioBlokeoaIraupenaMinutuak);
                var minutuak = Math.Max(1, (int)Math.Ceiling(geratzen.TotalMinutes));
                PasahitzaErroreMezua =
                    $"Saio askotan okerrak direla eta, kontua blokeatu egin da {minutuak} minutu arte.";
            }
            else
                PasahitzaErroreMezua = "Pasahitza okerra da. Saiatu berriro.";
        }
        catch (TursoExekuzioSalbuespena libEx)
        {
            PasahitzaErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                ?? "Datu-base errorea. Saiatu berriro.";
            _logger.LogError(libEx, "Ezarpenak: pasahitza egiaztatzean Turso errorea.");
        }
        catch (SQLiteException sqlEx)
        {
            PasahitzaErroreMezua = "Datu-base errorea. Saiatu berriro.";
            _logger.LogError(sqlEx, "Ezarpenak: pasahitza egiaztatzean SQLite errorea.");
        }
        catch (HttpRequestException httpEx)
        {
            PasahitzaErroreMezua = "Sare errorea: konexioa egiaztatu eta saiatu berriro.";
            _logger.LogError(httpEx, "Ezarpenak: sare errorea pasahitza egiaztatzean.");
        }
        catch (TaskCanceledException)
        {
            PasahitzaErroreMezua = "Eskaerak denbora muga gainditu du. Saiatu berriro.";
        }
        catch (Exception ex)
        {
            PasahitzaErroreMezua = "Ustekabeko errorea gertatu da.";
            _logger.LogError(ex, "Ezarpenak: pasahitza egiaztatzean ustekabeko errorea.");
        }
        finally
        {
            PasahitzaEkintza = false;
        }
    }

    [RelayCommand]
    private async Task PasahitzaAldatuAsync()
    {
        PasahitzaErroreMezua = null;

        if (string.IsNullOrWhiteSpace(PasahitzaBerria) || PasahitzaBerria.Length < 8)
        {
            PasahitzaErroreMezua = "Pasahitz berriak gutxienez 8 karaktere behar ditu.";
            return;
        }
        if (PasahitzaBerria != PasahitzaBerriaBerretsi)
        {
            PasahitzaErroreMezua = "Pasahitz berriak ez datoz bat. Egiaztatu eta saiatu berriro.";
            return;
        }
        if (_erabiltzaileId is null)
        {
            PasahitzaErroreMezua = "Saioa ez da aurkitu. Berrabiarazi aplikazioa.";
            return;
        }

        try
        {
            PasahitzaEkintza = true;
            var aldatua = await _erabiltzaileZerbitzua.AldatuPasahitzaAsync(
                _erabiltzaileId.Value, PasahitzaZaharra, PasahitzaBerria).ConfigureAwait(true);

            if (!aldatua)
            {
                PasahitzaErroreMezua = "Pasahitza okerra da. Ezin izan da aldatu.";
                return;
            }

            PasahitzaZaharra = string.Empty;
            PasahitzaBerria = string.Empty;
            PasahitzaBerriaBerretsi = string.Empty;
            PasahitzaEgiaztatuta = false;

            await BokadilloErakustzailea.SaiatuErakutsiAsync("Pasahitza ondo aldatu da.", _logger).ConfigureAwait(true);
        }
        catch (TursoExekuzioSalbuespena libEx)
        {
            PasahitzaErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                ?? "Datu-base errorea. Saiatu berriro.";
            _logger.LogError(libEx, "Ezarpenak: pasahitza aldatzean Turso errorea.");
        }
        catch (SQLiteException sqlEx)
        {
            PasahitzaErroreMezua = "Datu-base errorea. Saiatu berriro.";
            _logger.LogError(sqlEx, "Ezarpenak: pasahitza aldatzean SQLite errorea.");
        }
        catch (HttpRequestException httpEx)
        {
            PasahitzaErroreMezua = "Sare errorea: konexioa egiaztatu eta saiatu berriro.";
            _logger.LogError(httpEx, "Ezarpenak: sare errorea pasahitza aldatzean.");
        }
        catch (TaskCanceledException)
        {
            PasahitzaErroreMezua = "Eskaerak denbora muga gainditu du. Saiatu berriro.";
        }
        catch (Exception ex)
        {
            PasahitzaErroreMezua = "Ustekabeko errorea gertatu da.";
            _logger.LogError(ex, "Ezarpenak: pasahitza aldatzean ustekabeko errorea.");
        }
        finally
        {
            PasahitzaEkintza = false;
        }
    }

    private static bool DaAdministratzaileTaldea(int rola) =>
        rola == (int)ErabiltzaileRola.Administratzailea ||
        rola == (int)ErabiltzaileRola.ZuzendariNagusia;

    private void EzarriSektoreaKargoIkuspegia(Erabiltzailea erabiltzailea)
    {
        _daAdministratzaileTaldea = DaAdministratzaileTaldea(erabiltzailea.Rola);
        DaAdministratzailea = _daAdministratzaileTaldea;
        ErakutsiSektoreaKargoHautapenak = false;
        if (_daAdministratzaileTaldea)
        {
            FinkatutakoSektorearenEtiketa = AdministratzaileOrganizazioLehenetsia.Sektorea;
            FinkatutakoKargoarenEtiketa = SektoreaKargoarenHiztegia.LortuKargoarenEtiketa(
                EnpresakoLangileKargoa.AdministratzaileSistema);
            return;
        }

        HasieratuSektoreaKargoHautapenak(erabiltzailea);
        FinkatutakoSektorearenEtiketa = HautatutakoSektorea?.Etiketa ?? string.Empty;
        FinkatutakoKargoarenEtiketa = HautatutakoKargoa?.Etiketa ?? string.Empty;
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
            await BokadilloErakustzailea.SaiatuErakutsiAsync("Saioa itxita.", _logger).ConfigureAwait(true);
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
