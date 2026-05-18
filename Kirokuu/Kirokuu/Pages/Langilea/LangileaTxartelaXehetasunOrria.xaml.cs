using Kirokuu.ViewModels;

namespace Kirokuu.Pages;

public partial class LangileaTxartelaXehetasunOrria : ContentPage
{
    public LangileaTxartelaXehetasunOrria(LangileaTxartelaXehetasunViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
