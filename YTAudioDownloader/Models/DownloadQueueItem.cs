using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace YTAudioDownloader.Models;

public enum DownloadStatus
{
    Pending,
    Downloading,
    Converting,
    MetadataSearch,
    Completed,
    Error,
    Cancelled
}

public class DownloadQueueItem : INotifyPropertyChanged
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public string Url { get; set; } = string.Empty;

    private string _title = "...";
    public string Title
    {
        get => _title;
        set { _title = value; OnPropertyChanged(); }
    }

    private string _artist = string.Empty;
    public string Artist
    {
        get => _artist;
        set { _artist = value; OnPropertyChanged(); }
    }

    private string _duration = "--:--";
    public string Duration
    {
        get => _duration;
        set { _duration = value; OnPropertyChanged(); }
    }

    public string Format { get; set; } = "MP3";
    public AudioFormat AudioFormat { get; set; } = AudioFormat.Mp3;
    public AudioQuality AudioQuality { get; set; } = AudioQuality.VeryHigh;
    public string FilePath { get; set; } = string.Empty;

    private double _progress;
    public double Progress
    {
        get => _progress;
        set { _progress = value; OnPropertyChanged(); }
    }

    private DownloadStatus _status = DownloadStatus.Pending;
    public DownloadStatus Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusLabel));
            OnPropertyChanged(nameof(IsActive));
            OnPropertyChanged(nameof(IsCompleted));
            OnPropertyChanged(nameof(IsError));
            OnPropertyChanged(nameof(ProgressVisibility));
            OnPropertyChanged(nameof(StatusBadgeBg));
            OnPropertyChanged(nameof(StatusBadgeFg));
            OnPropertyChanged(nameof(IsSpinning));
        }
    }

    public string StatusLabel => Status switch
    {
        DownloadStatus.Pending => "En espera",
        DownloadStatus.Downloading => "Descargando",
        DownloadStatus.Converting => "Convirtiendo",
        DownloadStatus.MetadataSearch => "Metadatos",
        DownloadStatus.Completed => "Completado",
        DownloadStatus.Error => "Error",
        DownloadStatus.Cancelled => "Cancelado",
        _ => "Desconocido"
    };

    public SolidColorBrush StatusBadgeBg => Status switch
    {
        DownloadStatus.Downloading => new SolidColorBrush(Color.FromArgb(255, 222, 247, 255)),
        DownloadStatus.Converting => new SolidColorBrush(Color.FromArgb(255, 255, 248, 197)),
        DownloadStatus.MetadataSearch => new SolidColorBrush(Color.FromArgb(255, 243, 232, 255)),
        DownloadStatus.Completed => new SolidColorBrush(Color.FromArgb(255, 218, 251, 225)),
        DownloadStatus.Error or DownloadStatus.Cancelled => new SolidColorBrush(Color.FromArgb(255, 255, 235, 233)),
        _ => new SolidColorBrush(Color.FromArgb(255, 234, 238, 242))
    };

    public SolidColorBrush StatusBadgeFg => Status switch
    {
        DownloadStatus.Downloading => new SolidColorBrush(Color.FromArgb(255, 9, 105, 218)),
        DownloadStatus.Converting => new SolidColorBrush(Color.FromArgb(255, 154, 103, 0)),
        DownloadStatus.MetadataSearch => new SolidColorBrush(Color.FromArgb(255, 107, 33, 168)),
        DownloadStatus.Completed => new SolidColorBrush(Color.FromArgb(255, 26, 127, 55)),
        DownloadStatus.Error or DownloadStatus.Cancelled => new SolidColorBrush(Color.FromArgb(255, 207, 34, 46)),
        _ => new SolidColorBrush(Color.FromArgb(255, 87, 96, 106))
    };

    public bool IsActive => Status is DownloadStatus.Pending or DownloadStatus.Downloading
                            or DownloadStatus.Converting or DownloadStatus.MetadataSearch;
    public bool IsCompleted => Status == DownloadStatus.Completed;
    public bool IsError => Status is DownloadStatus.Error or DownloadStatus.Cancelled;
    public bool IsSpinning => Status is DownloadStatus.Downloading or DownloadStatus.Converting or DownloadStatus.MetadataSearch;

    public Visibility ProgressVisibility => IsActive ? Visibility.Visible : Visibility.Collapsed;

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    public System.Threading.CancellationTokenSource? CancellationTokenSource { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
