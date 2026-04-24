using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using YoutubeExplode;

namespace YTAudioDownloader.Services;

// Adaptado desde la idea de Soenneker.YouTube.Client para una app desktop sin DI externo.
public sealed class YouTubeClientUtil : IYouTubeClientUtil
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private YoutubeClient? _client;
    private HttpClient? _httpClient;
    private bool _disposed;

    public async ValueTask<YoutubeClient> GetAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_client is not null)
        {
            return _client;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_client is not null)
            {
                return _client;
            }

            _httpClient = BuildHttpClient();
            _client = new YoutubeClient(_httpClient);
            return _client;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static HttpClient BuildHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(45)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("es-ES,es;q=0.9,en;q=0.8");
        return client;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _httpClient?.Dispose();
        _gate.Dispose();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(YouTubeClientUtil));
        }
    }
}
