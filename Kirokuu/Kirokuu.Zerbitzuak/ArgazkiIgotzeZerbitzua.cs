using System.Text.Json;

namespace Kirokuu.Zerbitzuak;

/// <summary>
/// Uploads images to Cloudinary using unsigned upload preset (multipart).
/// Expects <c>CLOUDINARY_CLOUD_NAME</c> and <c>CLOUDINARY_UPLOAD_PRESET</c> in the environment.
/// </summary>
public sealed class ArgazkiIgotzeZerbitzua : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _disposeClient;

    public ArgazkiIgotzeZerbitzua(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _disposeClient = false;
    }

    /// <summary>
    /// Creates an instance that owns a new <see cref="HttpClient"/> with a 30s timeout.
    /// </summary>
    public ArgazkiIgotzeZerbitzua()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _disposeClient = true;
    }

    public void Dispose()
    {
        if (_disposeClient)
            _httpClient.Dispose();
    }

    /// <summary>
    /// Uploads a local file to Cloudinary and returns <c>secure_url</c>.
    /// </summary>
    public async Task<string> IgoArgazkiaAsync(string localFilePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(localFilePath))
            throw new ArgumentException("Path must not be empty.", nameof(localFilePath));

        var stream = File.OpenRead(localFilePath);
        try
        {
            return await IgoArgazkiaAsync(stream, Path.GetFileName(localFilePath), cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await stream.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Uploads a stream to Cloudinary and returns <c>secure_url</c>.
    /// </summary>
    public async Task<string> IgoArgazkiaAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var cloudName = Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME")?.Trim();
        if (string.IsNullOrWhiteSpace(cloudName))
            throw new InvalidOperationException("CLOUDINARY_CLOUD_NAME is not set.");

        var uploadPreset = Environment.GetEnvironmentVariable("CLOUDINARY_UPLOAD_PRESET")?.Trim();
        if (string.IsNullOrWhiteSpace(uploadPreset))
            throw new InvalidOperationException("CLOUDINARY_UPLOAD_PRESET is not set.");

        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        content.Add(streamContent, "file", string.IsNullOrWhiteSpace(fileName) ? "upload.png" : fileName);

        // Preset in query string: some HttpClient/multipart combinations omit form-data text parts reliably for Cloudinary.
        var requestUri =
            $"https://api.cloudinary.com/v1_1/{cloudName}/image/upload?upload_preset={Uri.EscapeDataString(uploadPreset)}";
        using var response = await _httpClient.PostAsync(requestUri, content, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException(
                $"Cloudinary upload failed with {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}");
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!doc.RootElement.TryGetProperty("secure_url", out var secureUrlElement))
            throw new InvalidOperationException("Cloudinary response did not contain secure_url.");

        var secureUrl = secureUrlElement.GetString();
        if (string.IsNullOrWhiteSpace(secureUrl))
            throw new InvalidOperationException("Cloudinary secure_url was empty.");

        return secureUrl;
    }
}
