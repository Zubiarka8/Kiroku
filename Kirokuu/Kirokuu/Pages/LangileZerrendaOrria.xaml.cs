namespace Kirokuu.Pages;

public partial class LangileZerrendaOrria : ContentPage
{
    public LangileZerrendaOrria(ViewModels.LangileZerrendaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
