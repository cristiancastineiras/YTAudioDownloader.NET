using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage.Pickers;
using YoutubeExplode.Videos;
using YTAudioDownloader.Models;
using YTAudioDownloader.Services;
using YTAudioDownloader.State;

namespace YTAudioDownloader.Pages;

public sealed partial class DownloadPage : Page
{
    private Video? _analyzedVideo;
    private CancellationTokenSource? _analyzeCts;

    public DownloadPage()
    {
        InitializeComponent();

        // Initialize tab visibility and metadata grid after all controls are loaded
        UpdateTabVisibility();
        UpdateMetadataVisibility();
    }

    private void UpdateTabVisibility()
    {
        // Show formato tab by default
        if (FormatoTabContent != null && MetadataTabContent != null && SalidaTabContent != null)
        {
            FormatoTabContent.Visibility = Visibility.Visible;
            MetadataTabContent.Visibility = Visibility.Collapsed;
            SalidaTabContent.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateMetadataVisibility()
    {
        // Update manual metadata grid visibility based on AutoMetadataSwitch
        if (ManualMetadataGrid != null && AutoMetadataSwitch != null)
        {
            ManualMetadataGrid.Visibility = AutoMetadataSwitch.IsOn ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        OutputFolderTextBox.Text = AppState.Instance.OutputFolder;
        AutoMetadataSwitch.IsOn = AppState.Instance.AutoMetadata;

        // Set default format/quality
        FormatComboBox.SelectedIndex = AppState.Instance.DefaultFormat switch
        {
            AudioFormat.Mp3 => 0,
            AudioFormat.Flac => 1,
            AudioFormat.Wav => 2,
            AudioFormat.Aac => 3,
            AudioFormat.Opus => 4,
            _ => 0
        };
        QualityComboBox.SelectedIndex = AppState.Instance.DefaultQuality switch
        {
            AudioQuality.Low => 0,
            AudioQuality.Medium => 1,
            AudioQuality.High => 2,
            AudioQuality.VeryHigh => 3,
            _ => 3
        };
    }

    private void OnUrlTextChanged(object sender, TextChangedEventArgs e)
    {
        var hasText = !string.IsNullOrWhiteSpace(VideoUrlTextBox.Text);
        AnalyzeButton.IsEnabled = hasText;
        if (!hasText)
        {
            VideoPreviewCard.Visibility = Visibility.Collapsed;
            DownloadButton.IsEnabled = false;
            _analyzedVideo = null;
        }
    }

    private async void OnAnalyzeClick(object sender, RoutedEventArgs e)
    {
        var url = VideoUrlTextBox.Text.Trim();
        if (string.IsNullOrEmpty(url)) return;

        _analyzeCts?.Cancel();
        _analyzeCts = new CancellationTokenSource();

        SetAnalyzingState(true);
        HideError();

        try
        {
            _analyzedVideo = await DownloadOrchestrator.GetVideoInfoAsync(url, _analyzeCts.Token);

            PreviewTitle.Text = _analyzedVideo.Title;
            PreviewArtist.Text = _analyzedVideo.Author.ChannelTitle;
            PreviewDuration.Text = _analyzedVideo.Duration?.ToString(@"m\:ss") ?? "--:--";
            VideoPreviewCard.Visibility = Visibility.Visible;
            DownloadButton.IsEnabled = true;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            ShowError($"No se pudo analizar el video: {ex.Message}");
            VideoPreviewCard.Visibility = Visibility.Collapsed;
            DownloadButton.IsEnabled = false;
            _analyzedVideo = null;
        }
        finally
        {
            SetAnalyzingState(false);
        }
    }

    private void OnDownloadClick(object sender, RoutedEventArgs e)
    {
        if (_analyzedVideo == null) return;

        var url = VideoUrlTextBox.Text.Trim();
        var audioFormat = GetSelectedFormat();
        var audioQuality = GetSelectedQuality();

        var item = new DownloadQueueItem
        {
            Url = url,
            Title = _analyzedVideo.Title,
            Artist = _analyzedVideo.Author.ChannelTitle,
            Duration = _analyzedVideo.Duration?.ToString(@"m\:ss") ?? "--:--",
            Format = GetFormatName(audioFormat),
            AudioFormat = audioFormat,
            AudioQuality = audioQuality,
        };

        AppState.Instance.Downloads.Add(item);
        DownloadOrchestrator.StartDownload(item);

        // Clear form
        VideoUrlTextBox.Text = "";
        VideoPreviewCard.Visibility = Visibility.Collapsed;
        DownloadButton.IsEnabled = false;
        _analyzedVideo = null;

        // Navigate to queue tab
        if (App.MainAppWindow is MainWindow mainWin)
            mainWin.NavigateToQueue();
    }

    private void OnTabChecked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton rb) return;

        // Check if controls are initialized (they might not be during XAML parsing)
        if (FormatoTabContent == null || MetadataTabContent == null || SalidaTabContent == null)
            return;

        var tag = rb.Tag?.ToString();
        FormatoTabContent.Visibility = tag == "formato" ? Visibility.Visible : Visibility.Collapsed;
        MetadataTabContent.Visibility = tag == "metadata" ? Visibility.Visible : Visibility.Collapsed;
        SalidaTabContent.Visibility = tag == "salida" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnAutoMetadataToggled(object sender, RoutedEventArgs e)
    {
        // Check if controls are initialized (they might not be during XAML parsing)
        if (ManualMetadataGrid == null || AutoMetadataSwitch == null)
            return;

        AppState.Instance.AutoMetadata = AutoMetadataSwitch.IsOn;
        ManualMetadataGrid.Visibility = AutoMetadataSwitch.IsOn ? Visibility.Collapsed : Visibility.Visible;
    }

    private async void OnBrowseFolderClick(object sender, RoutedEventArgs e)
    {
        var folderPicker = new FolderPicker();
        folderPicker.FileTypeFilter.Add("*");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(
            App.MainAppWindow ?? throw new InvalidOperationException("No window"));
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder is null) return;

        AppState.Instance.OutputFolder = folder.Path;
        OutputFolderTextBox.Text = folder.Path;
    }

    private void SetAnalyzingState(bool analyzing)
    {
        AnalyzeButton.IsEnabled = !analyzing;
        AnalyzeIcon.Glyph = analyzing ? "\uE895" : "\uE8F9"; // Sync vs Search
        AnalyzeText.Text = analyzing ? "Analizando..." : "Analizar";
    }

    private void ShowError(string message)
    {
        ErrorTextBlock.Text = message;
        ErrorCard.Visibility = Visibility.Visible;
    }

    private void HideError()
    {
        ErrorCard.Visibility = Visibility.Collapsed;
    }

    private AudioFormat GetSelectedFormat() => FormatComboBox.SelectedIndex switch
    {
        1 => AudioFormat.Flac,
        2 => AudioFormat.Wav,
        3 => AudioFormat.Aac,
        4 => AudioFormat.Opus,
        _ => AudioFormat.Mp3
    };

    private AudioQuality GetSelectedQuality() => QualityComboBox.SelectedIndex switch
    {
        0 => AudioQuality.Low,
        1 => AudioQuality.Medium,
        2 => AudioQuality.High,
        _ => AudioQuality.VeryHigh
    };

    private static string GetFormatName(AudioFormat fmt) => fmt switch
    {
        AudioFormat.Flac => "FLAC",
        AudioFormat.Wav => "WAV",
        AudioFormat.Aac => "AAC",
        AudioFormat.Opus => "Opus",
        _ => "MP3"
    };
}
