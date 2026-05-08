using CommunityToolkit.Mvvm.ComponentModel;

namespace Kirokuu.ViewModels;

public partial class AdministratzaileHasieraViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isKargatzean;

    [ObservableProperty]
    private string? _erroreMezua;

    [ObservableProperty]
    private string _testuOrokorra = "Hasiera. Estatistikak eta laburpenak laster egongo dira hemen.";
}
