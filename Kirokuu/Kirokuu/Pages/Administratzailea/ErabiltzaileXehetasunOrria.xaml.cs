namespace Kirokuu.Pages;

public partial class ErabiltzaileXehetasunOrria : ContentPage
{
    public ErabiltzaileXehetasunOrria(ViewModels.ErabiltzaileXehetasunViewModel ikuspegiModeloa)
    {
        InitializeComponent();
        BindingContext = ikuspegiModeloa;
    }
}
