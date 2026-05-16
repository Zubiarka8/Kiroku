namespace Kirokuu.Controls;

public partial class EgoeraBandarenView
{
    public static readonly BindableProperty EgoeraProperty =
        BindableProperty.Create(nameof(Egoera), typeof(string), typeof(EgoeraBandarenView), string.Empty);

    public string Egoera
    {
        get => (string)GetValue(EgoeraProperty);
        set => SetValue(EgoeraProperty, value);
    }

    public EgoeraBandarenView()
    {
        InitializeComponent();
    }
}
