namespace Kirokuu.ZerbitzuakSaioa;

public interface INabigazioNagusia
{
    Task JoanAppShelleraAsync(CancellationToken cancellationToken = default);

    Task JoanSaioHasieraraAsync(CancellationToken cancellationToken = default);
}
