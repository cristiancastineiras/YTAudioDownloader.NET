using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YTAudioDownloader.Models;
using YTAudioDownloader.Services;
using YTAudioDownloader.State;

namespace YTAudioDownloader.Services;

public static class DownloadOrchestrator
{
    private static readonly YouTubeAudioService _youTubeAudioService = new();
    private static readonly AudioConversionService _audioConversionService = new();
    private static readonly EnhancedMetadataService _enhancedMetadataService = new();
    private static readonly SongTagsService _songTagsService = new();
    private static readonly YouTubeClientUtil _youtubeClientUtil = new();

    public static async Task<Video> GetVideoInfoAsync(string url, CancellationToken ct = default)
    {
        var yt = await _youtubeClientUtil.GetAsync(ct);
        return await yt.Videos.GetAsync(url, ct);
    }

    public static void StartDownload(DownloadQueueItem item)
    {
        item.CancellationTokenSource = new CancellationTokenSource();
        _ = RunDownloadAsync(item);
    }

    public static void CancelDownload(DownloadQueueItem item)
    {
        item.CancellationTokenSource?.Cancel();
    }

    private static async Task RunDownloadAsync(DownloadQueueItem item)
    {
        var cts = item.CancellationTokenSource!;
        var state = AppState.Instance;

        try
        {
            var outputFolder = state.OutputFolder;
            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            // Download audio
            item.Status = DownloadStatus.Downloading;
            var downloadProgress = new Progress<double>(v => item.Progress = v);

            var downloadResult = await _youTubeAudioService.DownloadAudioAsync(
                item.Url, outputFolder, downloadProgress, cts.Token);

            item.Title = downloadResult.VideoTitle;

            // Convert audio
            item.Status = DownloadStatus.Converting;
            item.Progress = 0;
            var convSettings = new AudioConversionSettings(item.AudioFormat, item.AudioQuality);
            var convProgress = new Progress<double>(v => item.Progress = v);

            var outputPath = await _audioConversionService.ConvertAsync(
                downloadResult.FilePath, outputFolder, downloadResult.VideoTitle,
                convSettings, convProgress, cts.Token);

            var finalResult = downloadResult with
            {
                FilePath = outputPath,
                ContainerExtension = convSettings.FileExtension
            };

            TryDeleteSourceFile(downloadResult.FilePath, outputPath);
            item.FilePath = outputPath;
            item.Format = convSettings.FormatName;

            // Apply metadata
            if (state.AutoMetadata)
            {
                item.Status = DownloadStatus.MetadataSearch;
                await ApplyTagsAsync(item, finalResult, cts.Token);
            }

            item.Progress = 100;
            item.Status = DownloadStatus.Completed;

            if (state.OpenFolderOnComplete && !string.IsNullOrEmpty(outputPath))
            {
                Process.Start("explorer.exe", $"/select,\"{outputPath}\"");
            }
        }
        catch (OperationCanceledException)
        {
            item.Status = DownloadStatus.Cancelled;
        }
        catch (Exception ex)
        {
            item.Status = DownloadStatus.Error;
            item.ErrorMessage = ex.Message;
        }
    }

    private static async Task ApplyTagsAsync(
        DownloadQueueItem item,
        DownloadAudioResult result,
        CancellationToken ct)
    {
        try
        {
            var tags = await _enhancedMetadataService.SearchAsync(result.VideoTitle, item.Url, ct);
            await _songTagsService.ApplyTagsAsync(result.FilePath, tags, ct);
            if (!string.IsNullOrEmpty(tags.Artist)) item.Artist = tags.Artist;
            if (!string.IsNullOrEmpty(tags.Title)) item.Title = tags.Title;
        }
        catch
        {
            // Metadata errors don't fail the download
        }
    }

    private static void TryDeleteSourceFile(string source, string final)
    {
        try
        {
            if (!source.Equals(final, StringComparison.OrdinalIgnoreCase) && File.Exists(source))
                File.Delete(source);
        }
        catch { }
    }
}
