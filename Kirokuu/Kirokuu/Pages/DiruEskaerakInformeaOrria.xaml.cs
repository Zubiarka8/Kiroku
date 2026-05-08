namespace Kirokuu.Pages;

public partial class DiruEskaerakInformeaOrria : ContentPage
{
    public DiruEskaerakInformeaOrria(ViewModels.LangileDiruEskaeraInformeaViewModel ikuspegiModeloa)
    {
        InitializeComponent();
        BindingContext = ikuspegiModeloa;
    }
}
