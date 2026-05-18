namespace Kirokuu.Pages;

public partial class HasieraOrria : ContentPage
{
    public HasieraOrria(ViewModels.HasieraViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
