namespace Kirokuu.Pages;

public partial class EzarpenakOrria : ContentPage
{
    public EzarpenakOrria(ViewModels.EzarpenakViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
