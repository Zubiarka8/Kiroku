using Kirokuu.ViewModels;

namespace Kirokuu.Pages;

public partial class TxartelBerriaOrria : ContentPage
{
    public TxartelBerriaOrria(TxartelBerriaViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
