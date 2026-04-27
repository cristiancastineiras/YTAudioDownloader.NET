using System;
using System.Collections.ObjectModel;
using YTAudioDownloader.Models;

namespace YTAudioDownloader.State;

public class AppState
{
    public static AppState Instance { get; } = new();

    private AppState()
    {
        var musicFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        OutputFolder = System.IO.Directory.Exists(musicFolder)
            ? musicFolder
            : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    }

    public ObservableCollection<DownloadQueueItem> Downloads { get; } = new();

    public string OutputFolder { get; set; }

    public AudioFormat DefaultFormat { get; set; } = AudioFormat.Mp3;
    public AudioQuality DefaultQuality { get; set; } = AudioQuality.VeryHigh;
    public bool AutoMetadata { get; set; } = true;
    public bool CreateArtistSubfolders { get; set; } = true;
    public bool OpenFolderOnComplete { get; set; } = false;
    public bool UseGpuAcceleration { get; set; } = true;
    public bool LowPowerMode { get; set; } = false;
    public int MaxSimultaneousDownloads { get; set; } = 3;
}
