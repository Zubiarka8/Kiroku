using System.Collections;
using Kirokuu.DatuBasea.Ereduak;

namespace Kirokuu.Controls;

public partial class TxartelXehetasunGastuZerrendaView
{
    public static readonly BindableProperty GastuLerroakProperty =
        BindableProperty.Create(
            nameof(GastuLerroak),
            typeof(IEnumerable),
            typeof(TxartelXehetasunGastuZerrendaView),
            null);

    public IEnumerable? GastuLerroak
    {
        get => (IEnumerable?)GetValue(GastuLerroakProperty);
        set => SetValue(GastuLerroakProperty, value);
    }

    public TxartelXehetasunGastuZerrendaView()
    {
        InitializeComponent();
    }
}
