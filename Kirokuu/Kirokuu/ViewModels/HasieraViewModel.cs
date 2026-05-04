using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.ZerbitzuakSaioa;
using Microsoft.Extensions.Logging;

namespace Kirokuu.ViewModels;

public partial class HasieraViewModel : ObservableObject
{
    private readonly SaioaGordetzeZerbitzua _saioaGordetzeZerbitzua;
    private readonly ILogger<HasieraViewModel> _logger;

    public HasieraViewModel(SaioaGordetzeZerbitzua saioaGordetzeZerbitzua, ILogger<HasieraViewModel> logger)
    {
        _saioaGordetzeZerbitzua = saioaGordetzeZerbitzua ?? throw new ArgumentNullException(nameof(saioaGordetzeZerbitzua));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [ObservableProperty]
    private bool _isKargatzean;

    [ObservableProperty]
    private string? _erroreMezua;

    [ObservableProperty]
    private string _ongietorria = string.Empty;

    [RelayCommand]
    private async Task AgertzenDeneanAsync()
    {
        ErroreMezua = null;
        try
        {
            IsKargatzean = true;
            var izenak = await _saioaGordetzeZerbitzua.IrakurriIzenAbizenakAsync().ConfigureAwait(true);
            if (izenak is { } p)
                Ongietorria = $"Ongi etorri, {p.Izena} {p.Abizena}";
            else
                Ongietorria = "Ongi etorri";
        }
        catch (InvalidOperationException opEx)
        {
            ErroreMezua = "Eragiketa baliogabea. Berriz saiatu saioa hasita.";
            _logger.LogError(opEx, "Hasiera: saioa irakurtzean.");
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.";
            _logger.LogError(ex, "Hasiera: ustekabeko errorea.");
        }
        finally
        {
            IsKargatzean = false;
        }
    }
}
