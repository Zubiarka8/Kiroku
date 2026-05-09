namespace Kirokuu.Pages;

public partial class AplikazioIrekitzeOrria : ContentPage
{
    public AplikazioIrekitzeOrria(ViewModels.AplikazioIrekitzeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
