using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.DatuEreduak;
using Kirokuu.ZerbitzuakSaioa;
using Microsoft.Extensions.Logging;
using SQLite;

namespace Kirokuu.ViewModels;

public partial class LangileZerrendaViewModel : ObservableObject
{
    private readonly AutorizazioZerbitzua _autorizazioZerbitzua;
    private readonly ErabiltzaileZerbitzua _erabiltzaileZerbitzua;
    private readonly ILogger<LangileZerrendaViewModel> _logger;

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

    public ObservableCollection<ErabiltzaileLaburpena> Langileak { get; } = new();

    [RelayCommand]
    private async Task AgertzenDeneanAsync()
    {
        ErroreMezua = null;
        HutsaMezua = string.Empty;
        Langileak.Clear();

        try
        {
            IsKargatzean = true;
            if (!await _autorizazioZerbitzua.DaAdministratzaileaAsync().ConfigureAwait(true))
            {
                ErroreMezua = "Ez duzu baimenik atal honetan.";
                return;
            }

            var zerrenda = await _erabiltzaileZerbitzua.EskuratuLangileenLaburpenakAsync().ConfigureAwait(true);
            foreach (var lerroa in zerrenda)
                Langileak.Add(lerroa);

            if (Langileak.Count == 0)
                HutsaMezua = "Oraindik ez dago langilerik erregistratuta.";
        }
        catch (SQLiteException sqlEx)
        {
            ErroreMezua = "Datu-base errorea: ezin izan da irakurri. Saiatu berriro.";
            _logger.LogError(sqlEx, "Langile zerrenda: SQLite errorea.");
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
}
