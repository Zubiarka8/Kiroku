using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.DatuEreduak;
using Kirokuu.AplikazioZerbitzuak;
using Kirokuu.Zerbitzuak;
using Kirokuu.ZerbitzuakSaioa;

namespace Kirokuu.ViewModels;

public partial class EzarpenakViewModel
{
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

    [ObservableProperty] private bool _profilaGordetzen;

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
            var sektorId = _daAdministratzaileTaldea
                ? AdministratzaileOrganizazioLehenetsia.SektorearenIdentifikatzailea
                : HautatutakoSektorea!.Identifikatzailea;
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
                    sektorId,
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
        catch (Exception ex)
        {
            ViewModelSalbuespenTratatzailea.TratatuIdazketa(ex, m => ErroreMezua = m, _logger, "Ezarpenak profila");
        }
        finally
        {
            ProfilaGordetzen = false;
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
            FinkatutakoSektorearenEtiketa = SektoreaKargoarenHiztegia.LortuSektorearenEtiketa(
                AdministratzaileOrganizazioLehenetsia.SektorearenIdentifikatzailea);
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
}
