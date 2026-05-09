using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.DatuBasea.Ereduak;
using Kirokuu.Zerbitzuak;
using Kirokuu.ZerbitzuakSaioa;
using Microsoft.Extensions.Logging;
using SQLite;

namespace Kirokuu.ViewModels;

[QueryProperty(nameof(TxostenIdQuery), "TxostenId")]
public partial class LangileaTxartelaXehetasunViewModel : ObservableObject
{
    private readonly DatuBaseaZerbitzua _datuBaseaZerbitzua;
    private readonly AutorizazioZerbitzua _autorizazioZerbitzua;
    private readonly ILogger<LangileaTxartelaXehetasunViewModel> _logger;

    private int _txostenIdGordeta;

    public LangileaTxartelaXehetasunViewModel(
        DatuBaseaZerbitzua datuBaseaZerbitzua,
        AutorizazioZerbitzua autorizazioZerbitzua,
        ILogger<LangileaTxartelaXehetasunViewModel> logger)
    {
        _datuBaseaZerbitzua = datuBaseaZerbitzua ?? throw new ArgumentNullException(nameof(datuBaseaZerbitzua));
        _autorizazioZerbitzua = autorizazioZerbitzua ?? throw new ArgumentNullException(nameof(autorizazioZerbitzua));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [ObservableProperty]
    private string _txostenIdQuery = string.Empty;

    partial void OnTxostenIdQueryChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        if (!int.TryParse(Uri.UnescapeDataString(value.Trim()), out var id) || id <= 0)
            return;

        _txostenIdGordeta = id;
        _ = KargatuAsync();
    }

    [ObservableProperty]
    private bool _isKargatzean;

    [ObservableProperty]
    private string? _erroreMezua;

    [ObservableProperty]
    private string _helmuga = string.Empty;

    [ObservableProperty]
    private string _egoera = string.Empty;

    [ObservableProperty]
    private string _deskribapena = string.Empty;

    [ObservableProperty]
    private string _dataTestua = string.Empty;

    [ObservableProperty]
    private double _gastuenGuztira;

    [ObservableProperty]
    private string? _adminOharra;

    [ObservableProperty]
    private bool _adminOharraIkagarri;

    [ObservableProperty]
    private string? _argazkiUrl;

    [ObservableProperty]
    private bool _argazkiDago;

    public ObservableCollection<GastuLerroa> GastuLerroak { get; } = new();

    private async Task KargatuAsync()
    {
        ErroreMezua = null;
        GastuLerroak.Clear();
        if (_txostenIdGordeta <= 0)
            return;

        try
        {
            IsKargatzean = true;

            var erabiltzaileId = await _autorizazioZerbitzua.EskuratuOraingoErabiltzaileIdAsync().ConfigureAwait(true);
            if (erabiltzaileId is null)
            {
                ErroreMezua = "Saioa iraungitu da. Berriz hasi saioa.";
                return;
            }

            var txostena = await _datuBaseaZerbitzua.EskuratuBidaiaTxostenaIdzAsync(_txostenIdGordeta).ConfigureAwait(true);
            if (txostena is null)
            {
                ErroreMezua = "Txartela ez da aurkitu.";
                return;
            }

            if (txostena.ErabiltzaileId != erabiltzaileId.Value)
            {
                ErroreMezua = "Ez duzu baimenik txartel hau ikusteko.";
                return;
            }

            Helmuga = txostena.Helmuga;
            Egoera = txostena.Egoera;
            AdminOharra = txostena.AdminOharra;
            AdminOharraIkagarri = !string.IsNullOrWhiteSpace(txostena.AdminOharra);

            if (DateTime.TryParse(txostena.HasieraData, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces, out var data))
                DataTestua = data.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            else
                DataTestua = txostena.HasieraData;

            var lerroak = await _datuBaseaZerbitzua.ZerrendatuGastuLerroakTxostenIdzAsync(_txostenIdGordeta).ConfigureAwait(true);
            double guztira = 0;
            string? argazkia = null;
            foreach (var lerroa in lerroak)
            {
                GastuLerroak.Add(lerroa);
                guztira += lerroa.ZenbatekoaGuztira;
                if (string.IsNullOrWhiteSpace(argazkia) && !string.IsNullOrWhiteSpace(lerroa.TicketArgazkia))
                    argazkia = lerroa.TicketArgazkia;
                if (string.IsNullOrWhiteSpace(Deskribapena) && !string.IsNullOrWhiteSpace(lerroa.Oharrak))
                    Deskribapena = lerroa.Oharrak;
            }

            GastuenGuztira = guztira;
            ArgazkiUrl = argazkia;
            ArgazkiDago = !string.IsNullOrWhiteSpace(argazkia);
        }
        catch (TursoExekuzioSalbuespena libEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                ?? "Datu-base errorea: ezin izan da irakurri. Saiatu berriro.";
            _logger.LogError(libEx, "LangileaTxartelaXehetasun: Turso errorea.");
        }
        catch (KeyNotFoundException knfEx)
        {
            ErroreMezua = LibsqlErroreaErabiltzaileMezura.ZutabeEskemaMezua;
            _logger.LogError(knfEx, "LangileaTxartelaXehetasun: mapa errorea.");
        }
        catch (SQLiteException sqlEx)
        {
            ErroreMezua = "Datu-base errorea: ezin izan da irakurri. Saiatu berriro.";
            _logger.LogError(sqlEx, "LangileaTxartelaXehetasun: SQLite errorea.");
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "LangileaTxartelaXehetasun: ustekabeko errorea.");
        }
        finally
        {
            IsKargatzean = false;
        }
    }

    [RelayCommand]
    private async Task ItzeliAsync()
    {
        await Shell.Current.GoToAsync("..").ConfigureAwait(true);
    }
}
