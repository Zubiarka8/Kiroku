using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.ZerbitzuakSaioa;

namespace Kirokuu.ViewModels;

public partial class EzarpenakViewModel
{
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
        catch (Exception ex)
        {
            ViewModelSalbuespenTratatzailea.TratatuIrakurketa(ex, m => PasahitzaErroreMezua = m, _logger, "Ezarpenak pasahitza egiaztatu");
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
        catch (Exception ex)
        {
            ViewModelSalbuespenTratatzailea.TratatuIdazketa(ex, m => PasahitzaErroreMezua = m, _logger, "Ezarpenak pasahitza aldatu");
        }
        finally
        {
            PasahitzaEkintza = false;
        }
    }
}
