using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kirokuu.ZerbitzuakSaioa;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Plugin.Maui.Audio;

namespace Kirokuu.ViewModels;

public partial class AplikazioIrekitzeViewModel : ObservableObject
{
    private const string OihartzunFitxategiarenIzena = "05_deep_confident.wav";
    private const double AnimazioLehenetsiIraupenaSeg = 3.5;
    private const double SarreraZatia = 0.20;
    private const double IrteeraHasiera = 0.78;

    private readonly IAudioManager _audioManager;
    private readonly INabigazioNagusia _nabigazioNagusia;
    private readonly ILogger<AplikazioIrekitzeViewModel> _logger;
    private int _irekitzeSaioaHasiera;

    public AplikazioIrekitzeViewModel(
        IAudioManager audioManager,
        INabigazioNagusia nabigazioNagusia,
        ILogger<AplikazioIrekitzeViewModel> logger)
    {
        _audioManager = audioManager ?? throw new ArgumentNullException(nameof(audioManager));
        _nabigazioNagusia = nabigazioNagusia ?? throw new ArgumentNullException(nameof(nabigazioNagusia));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [ObservableProperty]
    private double _logoaIkusgarritasuna;

    [ObservableProperty]
    private double _logoaEskalaketa = 0.88;

    [ObservableProperty]
    private bool _isKargatzean = true;

    [ObservableProperty]
    private string? _erroreMezua;

    [RelayCommand]
    private async Task AgertzenDeneanAsync()
    {
        if (Interlocked.Exchange(ref _irekitzeSaioaHasiera, 1) != 0)
            return;

        ErroreMezua = null;
        IAudioPlayer? jotzailea = null;

        try
        {
            await using var paketeFluxua = await FileSystem.OpenAppPackageFileAsync(OihartzunFitxategiarenIzena).ConfigureAwait(true);
            using var memoria = new MemoryStream();
            await paketeFluxua.CopyToAsync(memoria).ConfigureAwait(true);
            memoria.Position = 0;

            jotzailea = _audioManager.CreatePlayer(memoria);

            var audioAmaitu = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            void Bukatu(object? _, EventArgs __)
            {
                audioAmaitu.TrySetResult();
            }

            jotzailea.PlaybackEnded += Bukatu;
            jotzailea.Error += (_, _) =>
            {
                _logger.LogWarning("Irekitze oihartzuna: erreprodukzio errorea.");
                audioAmaitu.TrySetResult();
            };

            var kronometroa = Stopwatch.StartNew();
            jotzailea.Play();

            var animazioa = AnimazioSinkronizatuAsync(jotzailea, audioAmaitu.Task, kronometroa, CancellationToken.None);

            try
            {
                await audioAmaitu.Task.WaitAsync(TimeSpan.FromSeconds(45)).ConfigureAwait(true);
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("Irekitze oihartzuna: erreprodukzioaren denbora muga.");
                jotzailea.Stop();
                audioAmaitu.TrySetResult();
            }

            jotzailea.PlaybackEnded -= Bukatu;

            await animazioa.ConfigureAwait(true);

            await _nabigazioNagusia.JoanSaioHasieraraAsync().ConfigureAwait(true);
        }
        catch (FileNotFoundException fnfEx)
        {
            ErroreMezua = "Audio fitxategia ez da aurkitu paketean.";
            _logger.LogError(fnfEx, "Irekitzea: oihartzun-fitxategia ez da aurkitu.");
            await JoanSaioHasieraSeguruAsync().ConfigureAwait(true);
        }
        catch (IOException ioEx)
        {
            ErroreMezua = "Fitxategi errorea: ezin izan da audioa irakurri.";
            _logger.LogError(ioEx, "Irekitzea: audio fluxua irakurtzean.");
            await JoanSaioHasieraSeguruAsync().ConfigureAwait(true);
        }
        catch (InvalidOperationException opEx)
        {
            ErroreMezua = "Eragiketa baliogabea. Saio hasiera orrira joaten.";
            _logger.LogError(opEx, "Irekitzea: erreprodukzio edo nabigazio baliogabea.");
            await JoanSaioHasieraSeguruAsync().ConfigureAwait(true);
        }
        catch (ArgumentException argEx)
        {
            ErroreMezua = "Baliogabeko audioaren konfigurazioa.";
            _logger.LogError(argEx, "Irekitzea: audioaren argumentu errorea.");
            await JoanSaioHasieraSeguruAsync().ConfigureAwait(true);
        }
        catch (OperationCanceledException ocEx)
        {
            _logger.LogError(ocEx, "Irekitzea: eragiketa eten egin da.");
            await JoanSaioHasieraSeguruAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea irekitzean.";
            _logger.LogError(ex, "Irekitzea: ustekabeko errorea.");
            await JoanSaioHasieraSeguruAsync().ConfigureAwait(true);
        }
        finally
        {
            jotzailea?.Dispose();
            IsKargatzean = false;
        }
    }

    private async Task JoanSaioHasieraSeguruAsync()
    {
        try
        {
            await _nabigazioNagusia.JoanSaioHasieraraAsync().ConfigureAwait(true);
        }
        catch (InvalidOperationException opEx)
        {
            ErroreMezua = "Eragiketa baliogabea nabigazioan.";
            _logger.LogError(opEx, "Irekitzea: nabigazio errorea.");
        }
        catch (Exception ex)
        {
            ErroreMezua = "Ustekabeko errorea nabigazioan.";
            _logger.LogError(ex, "Irekitzea: nabigazio ustekabekoa.");
        }
    }

    private async Task AnimazioSinkronizatuAsync(
        IAudioPlayer jotzailea,
        Task audioarenBukaera,
        Stopwatch kronometroa,
        CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (audioarenBukaera.IsCompleted)
                    break;

                double iraupena = jotzailea.Duration;
                double posizioa = jotzailea.CurrentPosition;

                double p;
                if (iraupena > 0.05)
                    p = Math.Clamp(posizioa / iraupena, 0, 1);
                else
                    p = Math.Clamp(kronometroa.Elapsed.TotalSeconds / AnimazioLehenetsiIraupenaSeg, 0, 1);

                double ikusgarritasuna;
                double eskalaketa;

                if (p < SarreraZatia)
                {
                    double t = p / SarreraZatia;
                    double eased = 1 - Math.Pow(1 - t, 3);
                    ikusgarritasuna = eased;
                    eskalaketa = 0.88 + 0.12 * eased;
                }
                else if (p < IrteeraHasiera)
                {
                    ikusgarritasuna = 1;
                    eskalaketa = 1;
                }
                else
                {
                    double t = (p - IrteeraHasiera) / (1 - IrteeraHasiera);
                    ikusgarritasuna = 1 - t;
                    eskalaketa = 1;
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    LogoaIkusgarritasuna = ikusgarritasuna;
                    LogoaEskalaketa = eskalaketa;
                });

                await Task.WhenAny(audioarenBukaera, Task.Delay(16, cancellationToken)).ConfigureAwait(true);
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                LogoaIkusgarritasuna = 0;
                LogoaEskalaketa = 1;
            });
        }
        catch (OperationCanceledException ocEx)
        {
            _logger.LogError(ocEx, "Irekitzea: animazioa eten egin da.");
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LogoaIkusgarritasuna = 0;
                LogoaEskalaketa = 1;
            });
        }
    }
}
