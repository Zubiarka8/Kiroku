namespace Kirokuu.Pages;

public partial class TxostenOnarpenXehetasunOrria : ContentPage
{
    public TxostenOnarpenXehetasunOrria(ViewModels.TxostenOnarpenXehetasunViewModel ikuspegiModeloa)
    {
        InitializeComponent();
        BindingContext = ikuspegiModeloa;
    }
}
