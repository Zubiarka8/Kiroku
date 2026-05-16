namespace Kirokuu.Controls;

public partial class OrriKargatzeEskema
{
    public static readonly BindableProperty IsKargatzeanProperty =
        BindableProperty.Create(nameof(IsKargatzean), typeof(bool), typeof(OrriKargatzeEskema), false);

    public static readonly BindableProperty ErroreMezuaProperty =
        BindableProperty.Create(nameof(ErroreMezua), typeof(string), typeof(OrriKargatzeEskema), null);

    public static readonly BindableProperty EdukiaProperty =
        BindableProperty.Create(nameof(Edukia), typeof(View), typeof(OrriKargatzeEskema), null);

    public OrriKargatzeEskema()
    {
        InitializeComponent();
    }

    public bool IsKargatzean
    {
        get => (bool)GetValue(IsKargatzeanProperty);
        set => SetValue(IsKargatzeanProperty, value);
    }

    public string? ErroreMezua
    {
        get => (string?)GetValue(ErroreMezuaProperty);
        set => SetValue(ErroreMezuaProperty, value);
    }

    public View? Edukia
    {
        get => (View?)GetValue(EdukiaProperty);
        set => SetValue(EdukiaProperty, value);
    }
}
