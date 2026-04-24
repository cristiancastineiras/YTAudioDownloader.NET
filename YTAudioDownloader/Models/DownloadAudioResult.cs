namespace YTAudioDownloader.Models;

public sealed record DownloadAudioResult(
    string VideoTitle,
    string FilePath,
    string ContainerExtension
);
