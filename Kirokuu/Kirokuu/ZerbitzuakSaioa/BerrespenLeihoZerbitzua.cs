namespace Kirokuu.ZerbitzuakSaioa;

public sealed class BerrespenLeihoZerbitzua
{
    public Task<bool> BerretsiAsync(string izenburua, string mezua, CancellationToken cancellationToken = default) =>
        MainThread.InvokeOnMainThreadAsync(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var orria = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (orria is null)
                return false;

            return await orria.DisplayAlert(izenburua, mezua, "Bai", "Ez").ConfigureAwait(true);
        });
}
