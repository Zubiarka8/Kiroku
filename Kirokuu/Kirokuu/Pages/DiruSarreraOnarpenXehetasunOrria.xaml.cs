namespace Kirokuu.Pages;

public partial class DiruSarreraOnarpenXehetasunOrria : ContentPage
{
    public DiruSarreraOnarpenXehetasunOrria(ViewModels.DiruSarreraOnarpenXehetasunViewModel ikuspegiModeloa)
    {
        InitializeComponent();
        BindingContext = ikuspegiModeloa;
    }
}
