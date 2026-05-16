using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.DatuEreduak;
using Kirokuu.Pages;
using Kirokuu.ZerbitzuakSaioa;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace Kirokuu.ViewModels;

public partial class TxostenGuztiekViewModel : TxostenZerrendaViewModelOinarria
{
    private readonly AutorizazioZerbitzua _autorizazioZerbitzua;
    private readonly DatuBaseaZerbitzua _datuBaseaZerbitzua;
    private readonly ILogger<TxostenGuztiekViewModel> _logger;

    public TxostenGuztiekViewModel(
        AutorizazioZerbitzua autorizazioZerbitzua,
        DatuBaseaZerbitzua datuBaseaZerbitzua,
        ILogger<TxostenGuztiekViewModel> logger)
    {
        _autorizazioZerbitzua = autorizazioZerbitzua ?? throw new ArgumentNullException(nameof(autorizazioZerbitzua));
        _datuBaseaZerbitzua = datuBaseaZerbitzua ?? throw new ArgumentNullException(nameof(datuBaseaZerbitzua));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [ObservableProperty]
    private bool _isKargatzean;

    [ObservableProperty]
    private string? _erroreMezua;

    public ObservableCollection<TxostenOnarpenLaburpena> TxostenGuztiak { get; } = new();

    [RelayCommand]
    private async Task IrekiTxostenXehetasunaAsync(TxostenOnarpenLaburpena? laburpena)
    {
        if (laburpena is null || laburpena.TxostenId <= 0)
            return;

        _logger.LogInformation("TxostenGuztiak: xehetasuna, id={TxostenId}", laburpena.TxostenId);

        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Shell.Current
                    .GoToAsync($"{nameof(TxostenOnarpenXehetasunOrria)}?TxostenId={laburpena.TxostenId}")
                    .ConfigureAwait(true);
            }).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ezin izan da xehetasuna ireki. Saiatu berriro.";
            _logger.LogError(ex, "TxostenGuztiak: nabigazio errorea.");
        }
    }

    [RelayCommand]
    private Task AgertzenDeneanAsync() =>
        KargatuAdminTxostenZerrendaAsync(
            _autorizazioZerbitzua,
            (sektoreId, ct) => _datuBaseaZerbitzua.ZerrendatuTxostenGuztiekAsync(sektoreId, ct),
            TxostenGuztiak,
            m => ErroreMezua = m,
            v => IsKargatzean = v,
            _logger,
            "TxostenGuztiak");
}
