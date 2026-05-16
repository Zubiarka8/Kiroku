namespace Kirokuu.Pages;

public partial class TxostenGuztiekOrria : ContentPage
{
    public TxostenGuztiekOrria(ViewModels.TxostenGuztiekViewModel ikuspegiModeloa)
    {
        InitializeComponent();
        BindingContext = ikuspegiModeloa;
    }
}
