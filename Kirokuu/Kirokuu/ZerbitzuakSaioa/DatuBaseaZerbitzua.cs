using Kirokuu.DatuBasea.Ereduak;
using Microsoft.Extensions.Logging;
using SQLite;

namespace Kirokuu.ZerbitzuakSaioa;

public sealed class DatuBaseaZerbitzua
{
    private const string GarapenAdminPosta = "admin@garapena.eus";

    private readonly SQLiteAsyncConnection _konexioa;
    private readonly PasahitzaZerbitzua _pasahitzaZerbitzua;
    private readonly ILogger<DatuBaseaZerbitzua> _logger;
    private readonly object _hasieratzeSarraila = new();
    private Task? _hasieratzeZeregina;

    public DatuBaseaZerbitzua(PasahitzaZerbitzua pasahitzaZerbitzua, ILogger<DatuBaseaZerbitzua> logger)
    {
        _pasahitzaZerbitzua = pasahitzaZerbitzua ?? throw new ArgumentNullException(nameof(pasahitzaZerbitzua));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var bidea = Path.Combine(FileSystem.AppDataDirectory, "kiroku_lokala.db3");
        _konexioa = new SQLiteAsyncConnection(bidea);
    }

    public SQLiteAsyncConnection EskuratuKonexioa() => _konexioa;

    public Task HasieratuAsync()
    {
        lock (_hasieratzeSarraila)
        {
            _hasieratzeZeregina ??= HasieratuBarneanAsync();
        }

        return _hasieratzeZeregina;
    }

    private async Task HasieratuBarneanAsync()
    {
        await _konexioa.CreateTableAsync<Erabiltzailea>().ConfigureAwait(false);
#if DEBUG
        await AdministratzaileLehenarenSeedGarapeneanAsync().ConfigureAwait(false);
#endif
    }

#if DEBUG
    private async Task AdministratzaileLehenarenSeedGarapeneanAsync()
    {
        try
        {
            var kopurua = await _konexioa.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Erabiltzailea WHERE Rola = ?",
                (int)ErabiltzaileRola.Administratzailea).ConfigureAwait(false);

            if (kopurua > 0)
                return;

            var (gatza, hash) = _pasahitzaZerbitzua.SortuGatzaEtaHash("Garapena123!");
            var admin = new Erabiltzailea
            {
                Izena = "Admin",
                Abizena = "Garapena",
                Posta = GarapenAdminPosta,
                PasahitzaGatza = gatza,
                PasahitzaHash = hash,
                Rola = (int)ErabiltzaileRola.Administratzailea,
                HutsuneakSaioan = 0
            };

            await _konexioa.InsertAsync(admin).ConfigureAwait(false);
            _logger.LogWarning(
                "Garapeneko administratzailea sortu da: {Posta}. Pasahitza aldatu produkzioa baino lehen.",
                GarapenAdminPosta);
        }
        catch (SQLiteException sqlEx)
        {
            _logger.LogError(sqlEx, "Administratzaile seed: SQLite errorea.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Administratzaile seed: ustekabeko errorea.");
        }
    }
#endif
}
