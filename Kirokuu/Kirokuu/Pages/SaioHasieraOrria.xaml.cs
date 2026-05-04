namespace Kirokuu.Pages;

public partial class SaioHasieraOrria : ContentPage
{
    public SaioHasieraOrria(ViewModels.SaioHasieraViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
