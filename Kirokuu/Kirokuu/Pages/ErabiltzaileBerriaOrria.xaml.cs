namespace Kirokuu.Pages;

public partial class ErabiltzaileBerriaOrria : ContentPage
{
    public ErabiltzaileBerriaOrria(ViewModels.ErabiltzaileBerriaViewModel ikuspegiModeloa)
    {
        InitializeComponent();
        BindingContext = ikuspegiModeloa;
    }
}
