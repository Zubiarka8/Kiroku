namespace Kirokuu.Pages;

public partial class MugimenduakOrria : ContentPage
{
    public MugimenduakOrria(ViewModels.MugimenduakViewModel ikuspegiModeloa)
    {
        InitializeComponent();
        BindingContext = ikuspegiModeloa;
    }
}
