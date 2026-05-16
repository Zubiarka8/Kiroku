namespace Kirokuu.Controls;

public partial class TxartelXehetasunHeroaView
{
    public static readonly BindableProperty EgoeraProperty =
        BindableProperty.Create(nameof(Egoera), typeof(string), typeof(TxartelXehetasunHeroaView), string.Empty);

    public static readonly BindableProperty HelmugaProperty =
        BindableProperty.Create(nameof(Helmuga), typeof(string), typeof(TxartelXehetasunHeroaView), string.Empty);

    public static readonly BindableProperty ZenbatekoaProperty =
        BindableProperty.Create(nameof(Zenbatekoa), typeof(double), typeof(TxartelXehetasunHeroaView), 0d);

    public static readonly BindableProperty DataTestuaProperty =
        BindableProperty.Create(nameof(DataTestua), typeof(string), typeof(TxartelXehetasunHeroaView), string.Empty);

    public string Egoera
    {
        get => (string)GetValue(EgoeraProperty);
        set => SetValue(EgoeraProperty, value);
    }

    public string Helmuga
    {
        get => (string)GetValue(HelmugaProperty);
        set => SetValue(HelmugaProperty, value);
    }

    public double Zenbatekoa
    {
        get => (double)GetValue(ZenbatekoaProperty);
        set => SetValue(ZenbatekoaProperty, value);
    }

    public string DataTestua
    {
        get => (string)GetValue(DataTestuaProperty);
        set => SetValue(DataTestuaProperty, value);
    }

    public TxartelXehetasunHeroaView()
    {
        InitializeComponent();
    }
}
