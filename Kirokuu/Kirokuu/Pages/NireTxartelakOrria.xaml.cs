using Kirokuu.ViewModels;

namespace Kirokuu.Pages;

public partial class NireTxartelakOrria : ContentPage
{
    public NireTxartelakOrria(NireTxartelakViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
