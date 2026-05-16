using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Kirokuu.DatuEreduak;
using Kirokuu.ZerbitzuakSaioa;
using Microsoft.Extensions.Logging;

namespace Kirokuu.ViewModels;

public abstract partial class TxostenZerrendaViewModelOinarria : ObservableObject
{
    protected async Task KargatuAdminTxostenZerrendaAsync(
        AutorizazioZerbitzua autorizazioZerbitzua,
        Func<int?, CancellationToken, Task<IReadOnlyList<TxostenOnarpenLaburpena>>> kargatu,
        ObservableCollection<TxostenOnarpenLaburpena> helburua,
        Action<string?> ezarriErroreMezua,
        Action<bool> ezarriKargatzean,
        ILogger log,
        string testuingurua,
        CancellationToken cancellationToken = default)
    {
        ezarriErroreMezua(null);
        helburua.Clear();

        try
        {
            ezarriKargatzean(true);
            if (!await autorizazioZerbitzua.DaAdministratzaileaAsync(cancellationToken).ConfigureAwait(true))
            {
                ezarriErroreMezua("Ez duzu baimenik atal honetan.");
                return;
            }

            var adminSektoreId = await autorizazioZerbitzua
                .EskuratuAdminSektoreIragazkiaAsync(cancellationToken)
                .ConfigureAwait(true);

            var txostenak = await kargatu(adminSektoreId, cancellationToken).ConfigureAwait(true);
            foreach (var t in txostenak)
                helburua.Add(t);
        }
        catch (Exception ex)
        {
            ViewModelSalbuespenTratatzailea.TratatuIrakurketa(ex, ezarriErroreMezua, log, testuingurua);
        }
        finally
        {
            ezarriKargatzean(false);
        }
    }
}
