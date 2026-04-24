using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using YTAudioDownloader.Configuration;

namespace YTAudioDownloader.Services;

/// <summary>
/// Servicio para descargar portadas de YouTube cuando no se encuentran en metadatos
/// </summary>
public sealed class CoverArtService
{
    private readonly HttpClient _httpClient;
    private readonly ApiEndpointsOptions _apiEndpoints;

    public CoverArtService(HttpClient? httpClient = null)
    {
        _apiEndpoints = AppConfiguration.Endpoints;
        _httpClient = httpClient ?? new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    /// <summary>
    /// Descarga la portada de YouTube basado en el ID del video
    /// Intenta resoluciones en orden: maxres → sd → hq → default
    /// </summary>
    public async Task<byte[]?> TryDownloadYouTubeCoverAsync(
        string videoId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(videoId))
        {
            return null;
        }

        // Intentar con diferentes resoluciones en orden de preferencia
        var resolutions = new[] { "maxresdefault", "sddefault", "hqdefault", "default" };

        foreach (var resolution in resolutions)
        {
            try
            {
                var imageUrl = $"{_apiEndpoints.YouTubeImageBaseUrl.TrimEnd('/')}/{videoId}/{resolution}.jpg";
                var response = await _httpClient.GetAsync(imageUrl, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var imageBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                    
                    // Validar que sea una imagen válida (al menos 1KB)
                    if (imageBytes.Length > 1024)
                    {
                        return imageBytes;
                    }
                }
            }
            catch (HttpRequestException)
            {
                // Continuar con la siguiente resolución
                continue;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Extrae el Video ID de una URL de YouTube
    /// </summary>
    public static string? ExtractVideoIdFromUrl(string youtubeUrl)
    {
        if (string.IsNullOrWhiteSpace(youtubeUrl))
        {
            return null;
        }

        // https://www.youtube.com/watch?v=XXXX
        if (youtubeUrl.Contains("watch?v="))
        {
            var startIndex = youtubeUrl.IndexOf("watch?v=") + 8;
            var endIndex = youtubeUrl.IndexOf("&", startIndex);
            if (endIndex == -1) endIndex = youtubeUrl.Length;
            return youtubeUrl.Substring(startIndex, endIndex - startIndex);
        }

        // https://youtu.be/XXXX
        if (youtubeUrl.Contains("youtu.be/"))
        {
            var startIndex = youtubeUrl.IndexOf("youtu.be/") + 9;
            var endIndex = youtubeUrl.IndexOf("?", startIndex);
            if (endIndex == -1) endIndex = youtubeUrl.Length;
            return youtubeUrl.Substring(startIndex, endIndex - startIndex);
        }

        // https://www.youtube.com/shorts/XXXX
        if (youtubeUrl.Contains("shorts/"))
        {
            var startIndex = youtubeUrl.IndexOf("shorts/") + 7;
            var endIndex = youtubeUrl.IndexOf("?", startIndex);
            if (endIndex == -1) endIndex = youtubeUrl.Length;
            return youtubeUrl.Substring(startIndex, endIndex - startIndex);
        }

        // https://www.youtube.com/embed/XXXX
        if (youtubeUrl.Contains("embed/"))
        {
            var startIndex = youtubeUrl.IndexOf("embed/") + 6;
            var endIndex = youtubeUrl.IndexOf("?", startIndex);
            if (endIndex == -1) endIndex = youtubeUrl.Length;
            return youtubeUrl.Substring(startIndex, endIndex - startIndex);
        }

        return null;
    }
}
