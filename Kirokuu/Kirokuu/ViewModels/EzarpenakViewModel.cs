using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.DatuEreduak;
using Kirokuu.AplikazioZerbitzuak;
using Kirokuu.Zerbitzuak;
using Kirokuu.ZerbitzuakSaioa;
using Microsoft.Extensions.Logging;

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

        SektoreaKargoHautapenLaguntzailea.BeteKargoenZerrenda(KargoenAukerak, value);
        HautatutakoKargoa = KargoenAukerak.FirstOrDefault();
    }

    [ObservableProperty] private bool _isKargatzean;
    [ObservableProperty] private string? _erroreMezua;

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
                (int)ErabiltzaileRola.Administratzailea => "Administrador",
                (int)ErabiltzaileRola.ZuzendariNagusia => "Director general (CEO)",
                _ => "Empleado"
            };

            EzarriSektoreaKargoIkuspegia(erabiltzailea);

            if (DateTime.TryParse(erabiltzailea.SorkuntzaData, null, System.Globalization.DateTimeStyles.RoundtripKind, out var data))
                SorkuntzaDataTestu = data.ToLocalTime().ToString("dd/MM/yyyy");
            else
                SorkuntzaDataTestu = erabiltzailea.SorkuntzaData;
        }
        catch (Exception ex)
        {
            ViewModelSalbuespenTratatzailea.TratatuIrakurketa(ex, m => ErroreMezua = m, _logger, "Ezarpenak");
        }
        finally
        {
            IsKargatzean = false;
        }
    }
}
