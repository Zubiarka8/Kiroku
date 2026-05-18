using Kirokuu.ViewModels;

namespace Kirokuu.Pages;

public partial class TxartelKanbanOrria : ContentPage
{
    public TxartelKanbanOrria(TxartelKanbanViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
