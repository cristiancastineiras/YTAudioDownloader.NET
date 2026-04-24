using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YTAudioDownloader.Models;
using YoutubeExplode;
using YoutubeExplode.Videos;

namespace YTAudioDownloader.Services;

public sealed class YouTubeAudioService : IDisposable
{
    private readonly IYouTubeClientUtil _youtubeClientUtil;

    public YouTubeAudioService()
    {
        _youtubeClientUtil = new YouTubeClientUtil();
    }

    public async Task<DownloadAudioResult> DownloadAudioAsync(
        string videoUrl,
        string outputFolder,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(videoUrl))
        {
            throw new ArgumentException("Debes indicar una URL de YouTube valida.", nameof(videoUrl));
        }

        if (string.IsNullOrWhiteSpace(outputFolder) || !Directory.Exists(outputFolder))
        {
            throw new DirectoryNotFoundException("La carpeta de salida no existe.");
        }

        var videoId = ResolveVideoId(videoUrl.Trim());

        var youtubeClient = await _youtubeClientUtil.GetAsync(cancellationToken);

        Video video;
        YoutubeExplode.Videos.Streams.StreamManifest manifest;
        try
        {
            video = await youtubeClient.Videos.GetAsync(videoId, cancellationToken);
            manifest = await youtubeClient.Videos.Streams.GetManifestAsync(videoId, cancellationToken);
        }
        catch (Exception ex) when (IsPotentialBadRequest(ex))
        {
            // Reintento único con ID normalizado para casos de URL ruidosa o endpoints temporales.
            var canonicalId = VideoId.Parse(videoId.Value);
            video = await youtubeClient.Videos.GetAsync(canonicalId, cancellationToken);
            manifest = await youtubeClient.Videos.Streams.GetManifestAsync(canonicalId, cancellationToken);
        }

        var preferredAudioStream = manifest
            .GetAudioOnlyStreams()
            .OrderByDescending(static stream => stream.Container.Name.Equals("mp4", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(stream => stream.Bitrate.BitsPerSecond)
            .FirstOrDefault();

        if (preferredAudioStream is null)
        {
            throw new InvalidOperationException("No se encontro un stream de audio descargable para ese video.");
        }

        var cleanTitle = SanitizeFileName(video.Title);
        var extension = preferredAudioStream.Container.Name;
        var outputFilePath = BuildUniqueOutputPath(outputFolder, cleanTitle, extension);

        var progressAdapter = progress is null
            ? null
            : new Progress<double>(value => progress.Report(Math.Clamp(value * 100d, 0d, 100d)));

        await youtubeClient.Videos.Streams.DownloadAsync(
            preferredAudioStream,
            outputFilePath,
            progressAdapter,
            cancellationToken);

        return new DownloadAudioResult(video.Title, outputFilePath, extension);
    }

    private static bool IsPotentialBadRequest(Exception exception)
    {
        if (exception is HttpRequestException httpEx && httpEx.StatusCode is not null)
        {
            return (int)httpEx.StatusCode.Value is 400 or 403;
        }

        return exception.Message.Contains("400", StringComparison.OrdinalIgnoreCase)
            || exception.Message.Contains("403", StringComparison.OrdinalIgnoreCase)
            || exception.Message.Contains("Bad Request", StringComparison.OrdinalIgnoreCase);
    }

    private static VideoId ResolveVideoId(string input)
    {
        if (TryParseVideoId(input, out var parsedVideoId))
        {
            return parsedVideoId;
        }

        if (!Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("La URL de YouTube no es valida.", nameof(input));
        }

        var host = uri.Host.ToLowerInvariant();
        if (host.Contains("youtu.be", StringComparison.Ordinal))
        {
            var fromPath = uri.AbsolutePath.Trim('/');
            if (TryParseVideoId(fromPath, out parsedVideoId))
            {
                return parsedVideoId;
            }
        }

        if (host.Contains("youtube.com", StringComparison.Ordinal))
        {
            var queryParams = ParseQueryString(uri.Query);
            if (queryParams.TryGetValue("v", out var vParam) && TryParseVideoId(vParam, out parsedVideoId))
            {
                return parsedVideoId;
            }

            var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 2 && (segments[0].Equals("shorts", StringComparison.OrdinalIgnoreCase) || segments[0].Equals("embed", StringComparison.OrdinalIgnoreCase)))
            {
                if (TryParseVideoId(segments[1], out parsedVideoId))
                {
                    return parsedVideoId;
                }
            }
        }

        throw new ArgumentException("No se pudo extraer el ID del video desde la URL proporcionada.", nameof(input));
    }

    private static bool TryParseVideoId(string value, out VideoId videoId)
    {
        try
        {
            videoId = VideoId.Parse(value);
            return true;
        }
        catch
        {
            videoId = default;
            return false;
        }
    }

    private static Dictionary<string, string> ParseQueryString(string query)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(query))
        {
            return values;
        }

        var raw = query.StartsWith('?') ? query[1..] : query;
        var pairs = raw.Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            values[key] = value;
        }

        return values;
    }

    private static string BuildUniqueOutputPath(string folder, string fileNameWithoutExtension, string extension)
    {
        var basePath = Path.Combine(folder, $"{fileNameWithoutExtension}.{extension}");
        if (!File.Exists(basePath))
        {
            return basePath;
        }

        var index = 1;
        string candidate;
        do
        {
            candidate = Path.Combine(folder, $"{fileNameWithoutExtension} ({index}).{extension}");
            index++;
        }
        while (File.Exists(candidate));

        return candidate;
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            builder.Append(invalidChars.Contains(character) ? '_' : character);
        }

        return builder.ToString().Trim();
    }

    public void Dispose()
    {
        _youtubeClientUtil.Dispose();
    }
}
