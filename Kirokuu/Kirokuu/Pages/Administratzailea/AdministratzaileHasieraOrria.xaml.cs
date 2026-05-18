namespace Kirokuu.Pages;

public partial class AdministratzaileHasieraOrria : ContentPage
{
    public AdministratzaileHasieraOrria(ViewModels.AdministratzaileHasieraViewModel ikuspegiModeloa)
    {
        InitializeComponent();
        BindingContext = ikuspegiModeloa;
    }
}
