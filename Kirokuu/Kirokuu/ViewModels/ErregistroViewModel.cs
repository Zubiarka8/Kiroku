using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.ZerbitzuakSaioa;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using SQLite;

namespace Kirokuu.ViewModels;

public partial class ErregistroViewModel : ObservableObject
{
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
    }

    [ObservableProperty]
    private string _izena = string.Empty;

    [ObservableProperty]
    private string _abizena = string.Empty;

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

    [RelayCommand]
    private async Task ErregistratuAsync()
    {
        ErroreMezua = null;
        if (string.IsNullOrWhiteSpace(Izena) || string.IsNullOrWhiteSpace(Abizena) ||
            string.IsNullOrWhiteSpace(Posta) || string.IsNullOrWhiteSpace(Pasahitza) ||
            string.IsNullOrWhiteSpace(PasahitzaBerretsi))
        {
            ErroreMezua = "Eremu bat edo gehiago hutsik daude. Bete beharrezko eremuak.";
            return;
        }

        if (!Posta.Contains('@', StringComparison.Ordinal))
        {
            ErroreMezua = "Posta baliogabea da.";
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
            var erabiltzailea = await _erabiltzaileZerbitzua.ErregistratuLangileaAsync(Izena, Abizena, Posta, Pasahitza).ConfigureAwait(true);
            await _saioaGordetzeZerbitzua.GordeAsync(erabiltzailea).ConfigureAwait(true);
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Toast.Make("Kontua sortu da. Ongi etorri!").Show().ConfigureAwait(true);
            }).ConfigureAwait(true);
            await _nabigazioNagusia.JoanAppShelleraAsync().ConfigureAwait(true);
        }
        catch (SQLiteException sqlEx) when (sqlEx.Result == SQLite3.Result.Constraint)
        {
            ErroreMezua = "Datu bikoiztua: posta hau dagoeneko erregistratuta dago.";
            _logger.LogWarning(sqlEx, "Erregistroa: murrizketa urratua.");
        }
        catch (SQLiteException sqlEx)
        {
            ErroreMezua = "Datu-base errorea: ezin izan da gorde. Saiatu berriro.";
            _logger.LogError(sqlEx, "Erregistroa: SQLite errorea.");
        }
        catch (InvalidOperationException opEx)
        {
            ErroreMezua = "Eragiketa baliogabea. Berriz saiatu saioa hasita.";
            _logger.LogError(opEx, "Erregistroa: nabigazio errorea.");
        }
        catch (ArgumentException argEx)
        {
            ErroreMezua = "Ezin izan da saioaren datuak gorde. Saiatu berriro edo berrabiarazi aplikazioa.";
            _logger.LogError(argEx, "Erregistroa: SecureStorage edo balio baliogabea.");
        }
        catch (NotSupportedException nsEx)
        {
            ErroreMezua = "Datu-base errorea: eragiketa ez da onartzen. Saiatu berriro.";
            _logger.LogError(nsEx, "Erregistroa: onartzen ez den eragiketa.");
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
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
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "Erregistroa: itzultzean ustekabeko errorea.");
        }
    }
}
