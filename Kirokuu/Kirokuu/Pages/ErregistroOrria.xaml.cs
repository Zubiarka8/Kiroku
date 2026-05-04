namespace Kirokuu.Pages;

public partial class ErregistroOrria : ContentPage
{
    public ErregistroOrria(ViewModels.ErregistroViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
