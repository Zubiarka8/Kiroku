namespace Kirokuu.Controls;

public partial class HutsikIkuspegia
{
    public static readonly BindableProperty MezuaProperty =
        BindableProperty.Create(nameof(Mezua), typeof(string), typeof(HutsikIkuspegia), string.Empty);

    public string Mezua
    {
        get => (string)GetValue(MezuaProperty);
        set => SetValue(MezuaProperty, value);
    }

    public HutsikIkuspegia()
    {
        InitializeComponent();
    }
}
